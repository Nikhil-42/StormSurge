using Godot;
using System.Collections.Generic;
using System.Linq;
using System;

namespace StormSurge;
public partial class GameManager : Node {
    // Singleton instance
    public static GameManager Instance => _instance;
    private static GameManager _instance = null;

    // ================= GLOBAL VARIABLES ================================
    public bool PrintDebug => _printDebug;
    public GameState Game => _game;

    public NotificationManager NotificationManager { get; private set; }
    public UI UI { get; private set; }

    [Export]
    private string regionStatsPath = "res://Library/regionstats.txt";
    [Export]
    private Json stormJson = GD.Load<Json>("res://Library/stormtree.json");
    [Export]
    private Json humanityJson = GD.Load<Json>("res://Library/globaltree.json");
    [Export]
    private Json regionJson = GD.Load<Json>("res://Library/regiontree.json");

    [Export]
    private bool _printDebug = true;

    [Export]
    public Globe Globe { get; private set; } = null;

    private GameState _game = null;

    private string currentScreen = "start_menu";
    private string currentOption = "";
    private string currentClick = "";
    // FIXME: where, when, and how to set and reset these variables in loop

    private AudioStreamPlayer intro;
    private AudioStreamPlayer loop;
    private AudioStreamPlayer ambience;
    private AudioStreamPlayer uiSounds;
    private AudioStreamPlayer regionSelectSound;

    Vector2 baseResolution = new Vector2(1280, 720);

    [Signal]
    public delegate void SolarChangedEventHandler(int newSolar);

    public float Solar {
        get => _game.Solar;
        set {
            if (_game.Solar != value) {
                _game.Solar = value;
                EmitSignal(SignalName.SolarChanged, value);
            }
        }
    }

    public override void _EnterTree() {
        if (_instance != null) {
            GD.PrintErr("WARNING: GameManager instance already exists!");  // debugging
        }
        _instance = this;

        if (Instance.PrintDebug) GD.Print("\nLoading region geographic data...");
        FileAccess file = FileAccess.Open(ProjectSettings.GlobalizePath(regionStatsPath), FileAccess.ModeFlags.Read);
        if (file == null) {
            GD.PrintErr("Failed to open region geographic data file: " + regionStatsPath);
            return;
        }

        Dictionary<string, RegionStats> regionsStats = new Dictionary<string, RegionStats>();
        string[] header = file.GetCsvLine();
        while (!file.EofReached()) {
            string[] line = file.GetCsvLine();
            if (line == null || line.Length == 0) continue;  // Skip empty lines
            RegionStats stats = RegionStats.FromCsvLine(line);
            regionsStats.Add(stats.name, stats);
        }

        GD.Print("GameManager entering tree");  // debugging
        _game = new GameState([.. regionsStats.Values.OrderBy(r => r.id)], stormJson, humanityJson, regionJson);

        intro = GetNode<AudioStreamPlayer>("IntroMusic");
        loop = GetNode<AudioStreamPlayer>("LoopMusic");
        ambience = GetNode<AudioStreamPlayer>("StormAmbience");
        uiSounds = GetNode<AudioStreamPlayer>("ClickSound");
        regionSelectSound = GetNode<AudioStreamPlayer>("SelectSound");

        UI = GetNode<UI>("Control2");

        // Notification system
        Json notificationJson = GD.Load<Json>("res://Library/notifications.json");
        NotificationManager = new NotificationManager(notificationJson);
        UI.SetNotificationManager(NotificationManager);
    }

    public override void _Ready() {
        // Music controls
        Timer timer = new Timer();
        AddChild(timer);
        timer.OneShot = true;
        timer.WaitTime = (float)intro.Stream.GetLength();

        timer.Connect("timeout", new Callable(this, nameof(OnIntroFinished)));
        intro.Play();
        timer.Start();

        // intro.Connect("finished", new Callable(this, nameof(OnIntroFinished)));
    }

    public override void _Process(double deltaTime) {
        Game.updateTime();

        // Update the humanity AIs		
        for (int i = 0; i < _game.RegionAIs.Length; i++) {
            _game.RegionAIs[i].Process(deltaTime, _game);
        }

        // Passive income generation, rate changes by sea level
        Solar += (float)(_game.PassiveIncome * (1f + (0.01f * _game.stormTree.Vars.Storm.SeaLevel)) * deltaTime);

        // Update globe water level
        Globe.WaterLevel = _game.stormTree.Vars.Storm.SeaLevel;
    }

    public void ApplyDamage(int regionID, float damage, DamageType type) {
        if (regionID == -1) {
            // GD.Print("Cannot apply damage to region -1 (Ocean)");
            return;
        } else if (regionID < -1 || regionID >= _game.RegionAIs.Length) {
            GD.PrintErr($"Invalid region ID: {regionID}");
            return;
        }

        _game.RegionAIs[regionID].ApplyDamage(damage, type);
        // if (PrintDebug) GD.Print($"Applying {damage} damage of type {type} to humanity AI in region {regionID}");
    }

    public RegionAI GetRegion(int id) {
        if (id < 0 || id >= _game.RegionAIs.Length) {
            return null;
        }
        return _game.RegionAIs[id];
    }

    private void OnIntroFinished() {  // Switch to looping music/sound tracks
        loop.Seek(0);
        ambience.Seek(0);
        loop.Play();
        ambience.Play();
    }

    public void PlayUIClickSound() {
        if (uiSounds != null && uiSounds.Stream != null) {
            uiSounds.Play();
        }
    }

    public void PlayRegionSelectSound() {
        if (regionSelectSound != null && regionSelectSound.Stream != null) {
            regionSelectSound.Play();
        }
    }

    public Vector2 ScaleUI(Vector2 toScale) {
        Vector2 viewportSize = GetViewport().GetVisibleRect().Size;

        float scaleFactor = Math.Min(viewportSize.Y / baseResolution.Y, viewportSize.X / baseResolution.X);

        return new Vector2(toScale.X * scaleFactor, toScale.Y * scaleFactor);
    }

    public int ScaleFont(int toScale) {
        Vector2 viewportSize = GetViewport().GetVisibleRect().Size;

        float scaleFactor = Math.Min(viewportSize.Y / baseResolution.Y, viewportSize.X / baseResolution.X);

        return (int)Math.Round(toScale * scaleFactor);
    }
}