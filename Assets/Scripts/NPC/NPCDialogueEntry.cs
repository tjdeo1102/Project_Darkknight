using System;

[Serializable]
public class NPCDialogueEntry
{
    public string id;
    public string displayName;
    public string[] introLines;
    public string choicePrompt;
    public NPCDialogueRoute[] routes;
}
