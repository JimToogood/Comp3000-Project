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
}
