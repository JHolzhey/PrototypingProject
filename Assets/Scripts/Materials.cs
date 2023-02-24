using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Materials
{
    public static float airDensity = 1.225f;
    public static Type[] materialTypes = {
        // Terrain:
        new Type("Black_Sand",  1400),
        new Type("Grass_A",     1400),
        new Type("Rock",        2771),
        
        // Buildings:
        new Type("Wood",        1000),
        new Type("Stone",       1000),
    };

    public static float CalcPenetration() {
        return 0;
    }

    public struct Type {
        readonly public string name;
        readonly public float density; // in kg/m^3

        public Type(string name, float density) {
            this.name = name;
            this.density = density;
        }
    }
}

// Material toughness correlates to better impact resistance but tensile strength doesn't
// (https://www.researchgate.net/publication/284985138_Tensile_Strength_versus_Toughness_of_Cement-Based_Materials_against_High-Velocity_Projectile_Impact)

// Material ductility means better resistance to projectile penetration (follows as above)
// (https://www.ncbi.nlm.nih.gov/pmc/articles/PMC8434575/)

// Material compresive strength is important for projectile penetration resistance
// (https://ascelibrary.org/doi/full/10.1061/%28ASCE%29ST.1943-541X.0003129)

// "Within the range of effective properties considered, the effective hardness index and elastic modulus"
// (https://www.sciencedirect.com/science/article/pii/S0734743X19307705)

// "In addition, it is demonstrated that the uniaxial compressive strength of the building material might be misleading as the identifier of a material 
// resistance against projectile impact because a significant difference in penetration resistance between materials with comparable compressive strength was found."
// (https://ascelibrary.org/doi/10.1061/%28ASCE%29MT.1943-5533.0003858)

/* -- MATERIAL DENSITIES: (g/cm^3) (https://www.engineeringtoolbox.com/density-solids-d_1265.html)
-- Chaff: 0.300
-- Cedar: 0.580
-- Oak: 0.750
-- Oil: 0.850
-- Animal Fat: 0.920
-- Coal: 0.950
-- Tar: 1.150
-- Pitch: 1.180
-- Resin: 1.200
-- Sand: 1.400
-- Charcoal: 2.050
-- Sulfur: 2.070
-- Tuff: 2.550
-- Slate: 2.600
-- Aluminum: 2.700
-- Quicklime: 3.350
-- Zinc: 7.130
-- Tin: 7.280
-- Iron: 7.870
-- Brass: 8.500
-- Bronze: 8.800
-- Copper: 8.960
-- Silver: 10.490
-- Lead: 11.350
-- Gold: 19.320
*/