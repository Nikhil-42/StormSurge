using Godot;
using System;

public partial class NotificationCard : CenterContainer
{
	[Export] public Label LabelNode;
	
	public void SetText(string text)
	{
		if (LabelNode != null)
			LabelNode.Text = text;
		else
			GD.PrintErr("Cannot set text — LabelNode is null.");
	}
}
