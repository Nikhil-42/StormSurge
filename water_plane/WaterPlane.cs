using Godot;

public partial class WaterPlane : Area3D
{
    [Export] public float RainSize = 3.0f;
    [Export] public float MouseSize = 5.0f;
    [Export(PropertyHint.Range, "0.0,10.0,0.1")] public float Viscosity = 1.0f;
    [Export(PropertyHint.Range, "0.0,10.0,0.1")] public float Diffusion = 1.0f;
    [Export] public Vector2I TextureSize = new Vector2I(1024, 512);
    [Export] public Texture2D Heightmap;

    struct SimulationState
    {
        public Rid wind;
        public Rid scalarField;

        public SimulationState(Rid wind, Rid scalarField)
        {
            this.wind = wind;
            this.scalarField = scalarField;
        }

        public SimulationState() : this(new Rid(), new Rid()) {}
    }

    private float t = 0.0f;
    private float maxT = 0.1f;

    private Texture2Drd scalar_texture;
    private Texture2Drd wind_texture;
    private int nextTexture = 0;

    private Vector4 addWavePoint;
    private Vector2 mousePos;
    private bool mouseLeftPressed = false;
    private bool mouseRightPressed = false;

    private RenderingDevice rd;
    private Rid shader;
    private Rid pipeline;

    private const int CIRCULAR_QUEUE_DEPTH = 2;
    private readonly SimulationState[] textureRds = new SimulationState[CIRCULAR_QUEUE_DEPTH];
    private readonly Rid[] textureSets = new Rid[CIRCULAR_QUEUE_DEPTH];
    private Rid heightmapSampler;
    private Rid heightmapSet;

    public override void _Ready()
    {
        RenderingServer.CallOnRenderThread(Callable.From(() => InitializeComputeCode(TextureSize)));

        ShaderMaterial material = GetNode<MeshInstance3D>("MeshInstance3D").MaterialOverride as ShaderMaterial;
        if (material != null)
        {
            scalar_texture = (Texture2Drd)material.GetShaderParameter("scalar_texture");
            if (scalar_texture == null)
                GD.PrintErr("Failed to get reference to scalar field texture");
            wind_texture = (Texture2Drd)material.GetShaderParameter("wind_texture");
            if (wind_texture == null)
                GD.PrintErr("Failed to get reference to wind field texture");
        }
    }

    public override void _ExitTree()
    {
        if (scalar_texture != null)
        {
            scalar_texture.TextureRdRid = new Rid();
        }
        if (wind_texture != null)
        {
            wind_texture.TextureRdRid = new Rid();
        }

        RenderingServer.CallOnRenderThread(Callable.From(FreeComputeResources));
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (Engine.IsEditorHint()) return;

        if (@event is InputEventMouseMotion || @event is InputEventMouseButton)
            mousePos = ((InputEventMouse)@event).GlobalPosition;

        if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.ButtonIndex == MouseButton.Left)
                mouseLeftPressed = mouseButton.Pressed;
            if (mouseButton.ButtonIndex == MouseButton.Right)
                mouseRightPressed = mouseButton.Pressed;
        }
    }

    private void CheckMousePos()
    {
        Camera3D camera = GetViewport().GetCamera3D();

        var parameters = PhysicsRayQueryParameters3D.Create(camera.ProjectRayOrigin(mousePos),
            camera.ProjectRayOrigin(mousePos) + camera.ProjectRayNormal(mousePos) * 100.0f);

        parameters.CollisionMask = 1;
        parameters.CollideWithBodies = false;
        parameters.CollideWithAreas = true;

        var result = GetWorld3D().DirectSpaceState.IntersectRay(parameters);
        if (result.Count > 0)
        {
            Vector3 pos = GlobalTransform.AffineInverse() * ((Vector3)result["position"]);
            addWavePoint.X = Mathf.Clamp(pos.X / 10.0f, -0.5f, 0.5f) * TextureSize.X + 0.5f * TextureSize.X;
            addWavePoint.Y = Mathf.Clamp(pos.Z / 5.0f, -0.5f, 0.5f) * TextureSize.Y + 0.5f * TextureSize.Y;
            addWavePoint.W = 1.0f;
        }
        else
        {
            addWavePoint = Vector4.Zero;
        }
    }

    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint())
        {
            addWavePoint.W = 0.0f;
        }
        else
        {
            CheckMousePos();
        }

        if (addWavePoint.W == 0.0f)
        {
            t += (float)delta;
            if (t > maxT)
            {
                t = 0;
                addWavePoint.X = GD.RandRange(0, TextureSize.X);
                addWavePoint.Y = GD.RandRange(0, TextureSize.Y);
                addWavePoint.Z = RainSize;
            }
            else
            {
                addWavePoint.Z = 0.0f;
            }
        }
        else
        {
            addWavePoint.Z = mouseLeftPressed ? MouseSize : 0.0f;
            addWavePoint.W = mouseRightPressed ? MouseSize : 0.0f;
        }

        nextTexture = (nextTexture + 1) % CIRCULAR_QUEUE_DEPTH;

        if (scalar_texture != null)
            scalar_texture.TextureRdRid = textureRds[nextTexture].scalarField;
        if (wind_texture != null)
            wind_texture.TextureRdRid = textureRds[nextTexture].wind;

        RenderingServer.CallOnRenderThread(Callable.From(() => RenderProcess(nextTexture, addWavePoint, TextureSize, (float)delta)));
    }

    private Rid CreateUniformSet(SimulationState simulationTextures)
    {
        var windUniform = new RDUniform
        {
            UniformType = RenderingDevice.UniformType.Image,
            Binding = 0
        };
        windUniform.AddId(simulationTextures.wind);

        var scalarFieldUniform = new RDUniform
        {
            UniformType = RenderingDevice.UniformType.Image,
            Binding = 1
        };
        scalarFieldUniform.AddId(simulationTextures.scalarField);
        return rd.UniformSetCreate([windUniform, scalarFieldUniform], shader, 1);
    }

    private void InitializeComputeCode(Vector2I initSize)
    {
        rd = RenderingServer.GetRenderingDevice();
        var shaderFile = GD.Load<RDShaderFile>("res://water_plane/shallow_water.glsl");
        shader = rd.ShaderCreateFromSpirV(shaderFile.GetSpirV());
        pipeline = rd.ComputePipelineCreate(shader);

        var heightmapSamplerState = new RDSamplerState
        {
            MinFilter = RenderingDevice.SamplerFilter.Linear,
            MagFilter = RenderingDevice.SamplerFilter.Linear
        };
        heightmapSampler = rd.SamplerCreate(heightmapSamplerState);

        var heightmapUniform = new RDUniform
        {
            UniformType = RenderingDevice.UniformType.SamplerWithTexture,
            Binding = 0
        };
        heightmapUniform.AddId(heightmapSampler);
        heightmapUniform.AddId(RenderingServer.TextureGetRdTexture(Heightmap.GetRid()));
        heightmapSet = rd.UniformSetCreate([heightmapUniform], shader, 0);

        var windTf = new RDTextureFormat
        {
            Format = RenderingDevice.DataFormat.R32G32B32A32Sfloat,
            TextureType = RenderingDevice.TextureType.Type2D,
            Width = (uint)initSize.X,
            Height = (uint)initSize.Y,
            Depth = 1,
            Mipmaps = 1,
            UsageBits = RenderingDevice.TextureUsageBits.SamplingBit |
                         RenderingDevice.TextureUsageBits.ColorAttachmentBit |
                         RenderingDevice.TextureUsageBits.StorageBit |
                         RenderingDevice.TextureUsageBits.CanUpdateBit |
                         RenderingDevice.TextureUsageBits.CanCopyToBit
        };

        var scalarTf = new RDTextureFormat
        {
            Format = RenderingDevice.DataFormat.R32G32B32A32Sfloat,
            TextureType = RenderingDevice.TextureType.Type2D,
            Width = (uint)initSize.X,
            Height = (uint)initSize.Y,
            Depth = 1,
            ArrayLayers = 1,
            Mipmaps = 1,
            UsageBits = RenderingDevice.TextureUsageBits.SamplingBit |
                         RenderingDevice.TextureUsageBits.ColorAttachmentBit |
                         RenderingDevice.TextureUsageBits.StorageBit |
                         RenderingDevice.TextureUsageBits.CanUpdateBit |
                         RenderingDevice.TextureUsageBits.CanCopyToBit
        };
        for (int i = 0; i < CIRCULAR_QUEUE_DEPTH; i++)
        {
            textureRds[i].wind = rd.TextureCreate(windTf, new RDTextureView(), []);
            textureRds[i].scalarField = rd.TextureCreate(scalarTf, new RDTextureView(), []);
            rd.TextureClear(textureRds[i].wind, new Color(0, 0, 0, 0), 0, 1, 0, 1);
            rd.TextureClear(textureRds[i].scalarField, new Color(0, 0, 0, 0), 0, 1, 0, 1);
            textureSets[i] = CreateUniformSet(textureRds[i]);
        }
    }

    private void RenderProcess(int index, Vector4 wavePoint, Vector2I texSize, float delta)
    {
        var pushConstant = new float[]
        {
            wavePoint.X, wavePoint.Y, wavePoint.Z, wavePoint.W,
            delta, Viscosity, Diffusion, 0.0f // Pad to 16 bytes
        };

        uint xGroups = (uint)((texSize.X - 1) / 16 + 1);
        uint yGroups = (uint)((texSize.Y - 1) / 16 + 1);

        var pushConstantBuffer = new byte[pushConstant.Length * sizeof(float)];
        System.Buffer.BlockCopy(pushConstant, 0, pushConstantBuffer, 0, pushConstant.Length * sizeof(float));

        var computeList = rd.ComputeListBegin();
        rd.ComputeListBindComputePipeline(computeList, pipeline);
        rd.ComputeListBindUniformSet(computeList, heightmapSet, 0);
        for (uint i = 0; i < CIRCULAR_QUEUE_DEPTH; i++)
        {
            rd.ComputeListBindUniformSet(computeList, textureSets[(index + CIRCULAR_QUEUE_DEPTH - i) % CIRCULAR_QUEUE_DEPTH], i+1);
        }
        rd.ComputeListSetPushConstant(computeList, pushConstantBuffer, (uint)pushConstantBuffer.Length);
        rd.ComputeListDispatch(computeList, xGroups, yGroups, 1);
        rd.ComputeListEnd();
    }

    private void FreeComputeResources()
    {
        if (heightmapSampler.IsValid)
            rd.FreeRid(heightmapSampler);

        for (int i = 0; i < CIRCULAR_QUEUE_DEPTH; i++)
        {
            if (textureRds[i].wind.IsValid)
                rd.FreeRid(textureRds[i].wind);
            if (textureRds[i].scalarField.IsValid)
                rd.FreeRid(textureRds[i].scalarField);
        }

        if (shader.IsValid)
            rd.FreeRid(shader);
    }
}