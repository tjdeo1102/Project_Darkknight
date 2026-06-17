using System;

[Serializable]
public class NPCDialogueRoute
{
    public string id;
    public string optionText;
    public string[] lines;
    public NPCDialogueReward reward;
}
