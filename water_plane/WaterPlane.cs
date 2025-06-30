using Godot;

public partial class WaterPlane : Area3D
{
    [Export] public float RainSize = 3.0f;
    [Export] public float MouseSize = 5.0f;
    [Export] public Vector2I TextureSize = new Vector2I(512, 512);
    [Export(PropertyHint.Range, "1.0,10.0,0.1")] public float Damp = 1.0f;

    private float t = 0.0f;
    private float maxT = 0.1f;

    private Texture2Drd texture;
    private int nextTexture = 0;

    private Vector4 addWavePoint;
    private Vector2 mousePos;
    private bool mousePressed = false;

    private RenderingDevice rd;
    private Rid shader;
    private Rid pipeline;
    private Rid[] textureRds = new Rid[3];
    private Rid[] textureSets = new Rid[3];

    public override void _Ready()
    {
        RenderingServer.CallOnRenderThread(Callable.From(() => InitializeComputeCode(TextureSize)));

        ShaderMaterial material = GetNode<MeshInstance3D>("MeshInstance3D").MaterialOverride as ShaderMaterial;
        if (material != null)
        {
            material.SetShaderParameter("effect_texture_size", TextureSize);
            texture = (Texture2Drd)material.GetShaderParameter("effect_texture");
        }
    }

    public override void _ExitTree()
    {
        if (texture != null)
        {
            texture.TextureRdRid = new Rid();
        }

        RenderingServer.CallOnRenderThread(Callable.From(FreeComputeResources));
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (Engine.IsEditorHint()) return;

        if (@event is InputEventMouseMotion || @event is InputEventMouseButton)
            mousePos = ((InputEventMouse)@event).GlobalPosition;

        if (@event is InputEventMouseButton mouseButton && mouseButton.ButtonIndex == MouseButton.Left)
            mousePressed = mouseButton.Pressed;
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
            addWavePoint.X = Mathf.Clamp(pos.X / 5.0f, -0.5f, 0.5f) * TextureSize.X + 0.5f * TextureSize.X;
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
            addWavePoint.Z = mousePressed ? MouseSize : 0.0f;
        }

        nextTexture = (nextTexture + 1) % 3;

        if (texture != null)
            texture.TextureRdRid = textureRds[nextTexture];

        RenderingServer.CallOnRenderThread(Callable.From(() => RenderProcess(nextTexture, addWavePoint, TextureSize, Damp)));
    }

    private Rid CreateUniformSet(Rid textureRd)
    {
        var uniform = new RDUniform
        {
            UniformType = RenderingDevice.UniformType.Image,
            Binding = 0
        };
        uniform.AddId(textureRd);
        return rd.UniformSetCreate([uniform], shader, 0);
    }

    private void InitializeComputeCode(Vector2I initSize)
    {
        rd = RenderingServer.GetRenderingDevice();
        var shaderFile = GD.Load<RDShaderFile>("res://water_plane/water_compute.glsl");
        shader = rd.ShaderCreateFromSpirV(shaderFile.GetSpirV());
        pipeline = rd.ComputePipelineCreate(shader);

        var tf = new RDTextureFormat
        {
            Format = RenderingDevice.DataFormat.R32Sfloat,
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

        for (int i = 0; i < 3; i++)
        {
            textureRds[i] = rd.TextureCreate(tf, new RDTextureView(), []);
            rd.TextureClear(textureRds[i], new Color(0, 0, 0, 0), 0, 1, 0, 1);
            textureSets[i] = CreateUniformSet(textureRds[i]);
        }
    }

    private void RenderProcess(int index, Vector4 wavePoint, Vector2I texSize, float damp)
    {
        var pushConstant = new float[]
        {
            wavePoint.X, wavePoint.Y, wavePoint.Z, wavePoint.W,
            damp, 0.0f
        };

        uint xGroups = (uint)((texSize.X - 1) / 8 + 1);
        uint yGroups = (uint)((texSize.Y - 1) / 8 + 1);

        Rid nextSet = textureSets[index];
        Rid currentSet = textureSets[(index + 2) % 3];
        Rid previousSet = textureSets[(index + 1) % 3];

        var pushConstantBuffer = new byte[pushConstant.Length * sizeof(float) + 2 * sizeof(int)];
        System.Buffer.BlockCopy(pushConstant, 0, pushConstantBuffer, 0, pushConstant.Length * sizeof(float));
        System.Buffer.BlockCopy(System.BitConverter.GetBytes(texSize.X), 0, pushConstantBuffer, pushConstant.Length * sizeof(float), sizeof(int));
        System.Buffer.BlockCopy(System.BitConverter.GetBytes(texSize.Y), 0, pushConstantBuffer, pushConstant.Length * sizeof(float) + sizeof(int), sizeof(int));

        var computeList = rd.ComputeListBegin();
        rd.ComputeListBindComputePipeline(computeList, pipeline);
        rd.ComputeListBindUniformSet(computeList, currentSet, 0);
        rd.ComputeListBindUniformSet(computeList, previousSet, 1);
        rd.ComputeListBindUniformSet(computeList, nextSet, 2);
        rd.ComputeListSetPushConstant(computeList, pushConstantBuffer, (uint)pushConstantBuffer.Length);
        rd.ComputeListDispatch(computeList, xGroups, yGroups, 1);
        rd.ComputeListEnd();
    }

    private void FreeComputeResources()
    {
        for (int i = 0; i < 3; i++)
        {
            if (textureRds[i].IsValid)
                rd.FreeRid(textureRds[i]);
        }

        if (shader.IsValid)
            rd.FreeRid(shader);
    }
}