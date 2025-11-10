using System;
using System.Collections.Generic;

[Serializable]
public class CharacterModel {
    public string id;          // ej: "SirRatoncius"
    public string tribe;       // "Oro", "Prisma", etc. (opcional/semántico)
    public string displayName; // nombre mostrado
    public string portrait;    // opcional: ruta/asset
}

[Serializable]
public class CharacterList {
    public List<CharacterModel> characters = new List<CharacterModel>();
}
