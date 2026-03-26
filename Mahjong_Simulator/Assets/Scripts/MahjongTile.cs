using UnityEngine;

public enum TileSuit { Characters, Bamboo, Dots, Winds, Dragons };
public enum WindType { None, North, South, East, West };
public enum DragonType { None, Green, Red, White };

[System.Serializable]
public class MahjongTile {
    public int id;
    public TileSuit suit;

    public int number;          // 1-9 for numbered tiles, otherwise 0
    public WindType wind;       // None for non wind tiles
    public DragonType dragon;   // None for non dragon tiles

    public bool IsSameTile(MahjongTile otherTile) {
        // If suits are different, tiles are not the same
        if (suit != otherTile.suit) { return false; }

        // If suits are the same, check if number/WindType/DragonType matches
        return suit switch {
            TileSuit.Characters or TileSuit.Bamboo or TileSuit.Dots => number == otherTile.number,
            TileSuit.Winds => wind == otherTile.wind,
            TileSuit.Dragons => dragon == otherTile.dragon,
            _ => false
        };
    }

    public bool IsNumbered() {
        return number != 0;
    }

    public bool IsSequentialTo(MahjongTile otherTile, int offset) {
        if (!IsNumbered() || !otherTile.IsNumbered()) { return false; }
        if (suit != otherTile.suit) { return false; }

        return number == otherTile.number + offset;
    }
}
