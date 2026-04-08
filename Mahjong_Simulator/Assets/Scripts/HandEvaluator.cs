using UnityEngine;
using System.Collections.Generic;
using System.Linq;


public static class HandEvaluator {
    private static List<Meld> totalMelds = new();
    private static List<Meld> foundMelds = new();
    private static List<MahjongTile> foundPair = new();
    private static List<MahjongTile> allTiles = new();
    private static List<Meld> bestMelds = new();


    public static bool IsWinningHand(Player player) {
        allTiles.Clear();
        EvaluateMeldsAndPair(player);

        if (totalMelds.Count == 4) {
            if (PonsAndKans()) {
                Debug.Log("!! WINNING HAND FOUND (PonsAndKans) !!");
                return true;
            }
        }
        else if (totalMelds.Count == 3) {
            // Combine hand and melds into total tiles
            allTiles = new List<MahjongTile>(player.hand);
            foreach (Meld meld in player.melds) {
                allTiles.AddRange(meld.tiles);
            }

            // Find chi count
            int chiCount = totalMelds.Count(m => m.type == CallType.Chi);

            if (chiCount == 0) {
                // Hands with 3 melds + no chis
                if (Dragonfly()) {
                    Debug.Log("!! WINNING HAND FOUND (Dragonfly) !!");
                    return true;
                }
                else if (WindflyOrWindyChi()) {
                    Debug.Log("!! WINNING HAND FOUND (Windfly) !!");
                    return true;
                }
            }
            else if (chiCount == 3) {                
                // Hands with 3 melds all chis
                if (WindflyOrWindyChi()) {
                    Debug.Log("!! WINNING HAND FOUND (WindyChi) !!");
                    return true;
                }
            }
        }
        else if (totalMelds.Count == 0) {
            // Hands with no melds
            if (Knitting(player)) {
                Debug.Log("!! WINNING HAND FOUND (Knitting) !!");
                return true;
            }
        }

        return false;
    }
    
    public static bool IsTenpai(Player player) {
        /* Tenpai:
        When a player is one tile away from winning (also known as fishing)
        */

        List<MahjongTile> uniqueTiles = TileSpawner.CreateFullTileSet()
            .GroupBy(t => t.id)
            .Select(g => g.First())
            .ToList();

        // Check every unique tile to see if adding it creates a winning hand
        foreach (MahjongTile tile in uniqueTiles) {
            player.hand.Add(tile);

            bool isWin = IsWinningHand(player);

            player.hand.Remove(tile);

            if (isWin) {
                return true;
            }
        }

        return false;
    }


    // -=-=- HANDS -=-=-
    private static bool PonsAndKans() {
        /* Pons And Kans:
        Four Pons/Kans (one of which can be replaced by a Chi) plus a pair, all of a single suit (plus winds and dragons)
        */

        if (totalMelds.Count(m => m.type == CallType.Chi) > 1) { return false; }    // If hand has more than 1 Chi, hand cant be valid
        if (foundPair.Count != 2) { return false; }     // Check that pair was found

        List<MahjongTile> allStandardTiles = new();
        foreach (Meld meld in totalMelds) {
            allStandardTiles.AddRange(meld.tiles);
        }

        allStandardTiles.AddRange(foundPair);

        // Check all tiles are the same suit (plus winds and dragons)
        return SuitCountExcludingHonours(allStandardTiles) == 1;
    }

    private static bool Knitting(Player player) {
        /* Knitting:
        Four sets of three tiles, each set contains the same numbered tile from every suit (e.g. Three of Bamboo,
        Three of Dots and Three of Characters) plus a Pair, which follows the same rules as the sets, except it
        is missing any one of the three suits
        */

        List<MahjongTile> tiles = new List<MahjongTile>(player.hand);

        // Group tiles by number
        List<IGrouping<int, MahjongTile>> groups = tiles.GroupBy(t => t.number).ToList();

        int tripleSets = 0;
        int pairSets = 0;

        foreach (IGrouping<int, MahjongTile> group in groups) {
            List<MahjongTile> groupTiles = group.ToList();

            if (!groupTiles.All(t => t.IsNumbered())) { return false; }     // Knitting has no winds or dragons

            List<TileSuit> suits = groupTiles
                .Select(t => t.suit)
                .Distinct()
                .ToList();
            
            if (groupTiles.Count == 3) {
                if (suits.Count != 3) { return false; }
                tripleSets++;
            } else if (groupTiles.Count == 2) {
                if (suits.Count != 2) { return false; }
                pairSets++;
            } else {
                return false;
            }
        }

        return tripleSets == 4 && pairSets == 1;
    }

    private static bool Dragonfly() {
        /* Dragonfly:
        One of each Dragon, a Pon/Kan in all suits plus a pair in any suit
        */

        // Check that all 3 melds are different suits
        if (SuitCountExcludingHonours(totalMelds.SelectMany(m => m.tiles).ToList()) != 3) { return false; }
        if (foundPair.Count != 2) { return false; }     // Check that pair was found

        int greenCount = 0;
        int redCount = 0;
        int whiteCount = 0;

        foreach (MahjongTile tile in allTiles) {
            if (tile.suit != TileSuit.Dragons) { continue; }

            if (tile.dragon == DragonType.Green) { greenCount++; }
            if (tile.dragon == DragonType.Red) { redCount++; }
            if (tile.dragon == DragonType.White) { whiteCount++; }
        }

        return greenCount == 1 && redCount == 1 && whiteCount == 1;
    }

    private static bool WindflyOrWindyChi() {
        /* Windfly:
        One of each Wind plus a Pon/Kan in each of the three suits, one of the Winds must also be paired
           Windy Chi:
        One of each Wind plus a Chi in each of the three suits, one of the Winds must also be paired

        (Because meld type checks are already done in IsWinningHand() the check for these two hands becomes identical)
        */

        // Check that all 3 melds are different suits
        if (SuitCountExcludingHonours(totalMelds.SelectMany(m => m.tiles).ToList()) != 3) { return false; }

        int northCount = 0;
        int southCount = 0;
        int eastCount = 0;
        int westCount = 0;

        foreach (MahjongTile tile in allTiles) {
            if (tile.suit != TileSuit.Winds) { continue; }

            if (tile.wind == WindType.North) { northCount++; }
            if (tile.wind == WindType.South) { southCount++; }
            if (tile.wind == WindType.East) { eastCount++; }
            if (tile.wind == WindType.West) { westCount++; }
        }

        if (northCount == 0 || southCount == 0 || eastCount == 0 || westCount == 0) { return false; }

        return northCount + southCount + eastCount + westCount == 5;
    }


    // -=-=- HELPERS -=-=-
    private static void EvaluateMeldsAndPair(Player player) {
        foundMelds.Clear();
        totalMelds.Clear();
        foundPair.Clear();

        List<MahjongTile> tiles = new List<MahjongTile>(player.hand)
            .OrderBy(t => t.suit)
            .ThenBy(t => t.number)
            .ToList();

        for (int i = 0; i < tiles.Count; i++) {
            for (int j = i + 1; j < tiles.Count; j++) {
                // Only check matching pairs
                if (!tiles[i].IsSameTile(tiles[j])) { continue; }

                MahjongTile tileA = tiles[i];
                MahjongTile tileB = tiles[j];

                // Remove found pair from remaining tiles
                List<MahjongTile> remaining = new List<MahjongTile>(tiles);
                remaining.Remove(tileA);
                remaining.Remove(tileB);

                List<Meld> currentMelds = new();

                bestMelds.Clear();
                FindPossibleMelds(remaining, currentMelds);

                if (bestMelds.Count > foundMelds.Count) {
                    foundMelds = new List<Meld>(bestMelds);
                    foundPair = new List<MahjongTile>{ tileA, tileB };
                }
            }
        }

        // Combine players melds with found melds to get all melds
        totalMelds = new List<Meld>(player.melds);
        totalMelds.AddRange(foundMelds);
    }

    private static void FindPossibleMelds(List<MahjongTile> tiles, List<Meld> currentMelds) {
        // Less than 3 tiles cant form a meld
        if (tiles.Count < 3) {
            if (currentMelds.Count > bestMelds.Count) {
                bestMelds = new List<Meld>(currentMelds);
            }
            return;
        }

        MahjongTile firstTile = tiles[0];

        // Try Pon
        if (tiles.Count(t => t.IsSameTile(firstTile)) >= 3) {
            List<MahjongTile> remaining = new List<MahjongTile>(tiles);
            List<MahjongTile> ponTiles = new();

            int removed = 0;
            for (int i = remaining.Count - 1; i >= 0; i--) {
                if (remaining[i].IsSameTile(firstTile)) {
                    ponTiles.Add(remaining[i]);
                    remaining.RemoveAt(i);

                    removed++;
                    // If 3 have been removed then a valid Pon has been found
                    if (removed == 3) { break; }
                }
            }

            currentMelds.Add(new Meld(ponTiles, CallType.Pon));

            FindPossibleMelds(remaining, currentMelds);
            currentMelds.RemoveAt(currentMelds.Count - 1);
        }

        // Try Chi
        if (firstTile.IsNumbered()) {
            for (int i = 1; i < tiles.Count; i++) {
                if (!tiles[i].IsSequentialTo(firstTile, 1)) { continue; }

                for (int j = i + 1; j < tiles.Count; j++) {
                    if (!tiles[j].IsSequentialTo(firstTile, 2)) { continue; }

                    List<MahjongTile> remaining = new List<MahjongTile>(tiles);
                    remaining.Remove(firstTile);
                    remaining.Remove(tiles[i]);
                    remaining.Remove(tiles[j]);

                    currentMelds.Add(new Meld(
                        new List<MahjongTile> { firstTile, tiles[i], tiles[j] },
                        CallType.Chi
                    ));

                    FindPossibleMelds(remaining, currentMelds);
                    currentMelds.RemoveAt(currentMelds.Count - 1);
                }
            }
        }

        if (currentMelds.Count > bestMelds.Count) {
            bestMelds = new List<Meld>(currentMelds);
        }
    }

    private static int SuitCountExcludingHonours(List<MahjongTile> tiles) {
        // Count how many unique suits are in tiles (excluding winds and dragons)
        List<TileSuit> suits = tiles
            .Where(t => t.IsNumbered())
            .Select(t => t.suit)
            .Distinct()
            .ToList();
        
        return suits.Count;
    }

    private static int SuitCount(List<MahjongTile> tiles) {
        // Count how many unique suits are in tiles
        List<TileSuit> suits = tiles
            .Select(t => t.suit)
            .Distinct()
            .ToList();
        
        return suits.Count;
    }
}
