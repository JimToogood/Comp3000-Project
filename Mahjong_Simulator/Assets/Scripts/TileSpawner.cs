using UnityEngine;
using System.Collections.Generic;

public static class TileSpawner {
    public static List<MahjongTile> CreateFullTileSet() {
        List<MahjongTile> tiles = new();
        int nextTileID = 0;
        
        CreateSuit(TileSuit.Bamboo, tiles, ref nextTileID);
        CreateSuit(TileSuit.Characters, tiles, ref nextTileID);
        CreateSuit(TileSuit.Dots, tiles, ref nextTileID);

        CreateDragons(tiles, ref nextTileID);
        CreateWinds(tiles, ref nextTileID);

        return tiles;
    }

    private static void CreateSuit(TileSuit suit, List<MahjongTile> tiles, ref int nextTileID) {
        // Spawn tiles 1-9 in given suit (x4)
        for (int i = 0; i < 9; i++) {
            for (int j = 0; j < 4; j++) {
                // Add tile to list
                tiles.Add(new MahjongTile{
                    id = nextTileID,
                    suit = suit,
                    number = i + 1,
                    wind = WindType.None,
                    dragon = DragonType.None
                });
                nextTileID++;
            }
        }
    }

    private static void CreateDragons(List<MahjongTile> tiles, ref int nextTileID) {
        // Spawn all 3 dragons (x4)
        for (int i = 0; i < 3; i++) {
            for (int j = 0; j < 4; j++) {
                // Add tile to list
                tiles.Add(new MahjongTile{
                    id = nextTileID,
                    suit = TileSuit.Dragons,
                    number = 0,
                    wind = WindType.None,
                    dragon = (DragonType)i + 1
                });
                nextTileID++;
            }
        }
    }
    
    private static void CreateWinds(List<MahjongTile> tiles, ref int nextTileID) {
        // Spawn all 4 winds (x4)
        for (int i = 0; i < 4; i++) {
            for (int j = 0; j < 4; j++) {
                // Add tile to list
                tiles.Add(new MahjongTile{
                    id = nextTileID,
                    suit = TileSuit.Winds,
                    number = 0,
                    wind = (WindType)i + 1,
                    dragon = DragonType.None
                });
                nextTileID++;
            }
        }
    }
}
