using System;
using System.Collections.Generic;

[Serializable]
public class Effect {
    public string resource; // coincide con ResourceType names
    public int delta;
}

[Serializable]
public class OptionData {
    public string text;
    public List<Effect> effects = new List<Effect>();
    public List<string> setFlags = new List<string>();
    public string next; // id de la carta siguiente 
}

[Serializable]
public class CardModel {
    public string id;
    public string title;
    public string description;
    public string character;     // id del personaje (coincide con characters.json)
    public int tribeId = -1;     // 0..4 según ResourceType
    public bool isTutorial = false; // true para las 5 primeras cartas del tutorial
    public bool isEnding = false; // true para cartas de ending (finales)

    public OptionData optionA;
    public OptionData optionB;
    public List<string> requiredFlags = new List<string>();
    public List<string> requiredResourceConditions = new List<string>(); // ej: "Prisma>60"
    public List<string> tags = new List<string>(); // "crisis" para zonas de calor
    public int weight = 1;
}

[Serializable]
public class CardList {
    public List<CardModel> cards = new List<CardModel>();
}
