using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Materials
{
    public static Type[] materialTypes = {
        // Terrain:
        new Type("Black_Sand", 1.0f),
        new Type("Grass_A", 1.0f),
        new Type("Rock", 1.0f),
        
        // Buildings:
        new Type("Wood", 1.0f),
        new Type("Stone", 1.0f),
    };

    public struct Type {
        readonly public string name;
        readonly public float density;

        public Type(string name, float density) {
            this.name = name;
            this.density = density;
        }
    }
}
