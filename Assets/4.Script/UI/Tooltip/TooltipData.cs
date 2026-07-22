using System.Collections.Generic;
using UnityEngine;


public interface ITooltipProvider
{
    TooltipData GetTooltipData(CharacterStats viewer = null);
}


public class TooltipData
{
    public string title;
    public Color titleColor = Color.white;

    public string subtitle;
    public string description;

    public List<string> stats = new List<string>();
    public List<string> requirements = new();

    public string footer; // t.ex price eller requirements
    public bool showFooter = false;
}
