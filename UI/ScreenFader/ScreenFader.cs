using Godot;
using System.Threading.Tasks;

// ScreenFader.cs
public partial class ScreenFader : CanvasLayer
{
	private ColorRect _fadeRect;

	public override void _Ready()
	{
		_fadeRect = GetNode<ColorRect>("FadeRect");

		// start transparent
		_fadeRect.Modulate = new Color(0, 0, 0, 0);
	}

	public async Task FadeOut(float duration = 0.5f)
	{
		var tween = GetTree().CreateTween();
		tween.TweenProperty(_fadeRect, "modulate:a", 1f, duration);
		await ToSignal(tween, "finished");
	}

	public async Task FadeIn(float duration = 0.5f)
	{
		var tween = GetTree().CreateTween();
		tween.TweenProperty(_fadeRect, "modulate:a", 0f, duration);
		await ToSignal(tween, "finished");
	}

	public void SetInstantBlack()
	{
		_fadeRect.Modulate = new Color(0, 0, 0, 1);
	}
}
