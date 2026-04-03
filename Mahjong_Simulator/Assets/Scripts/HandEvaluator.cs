using UnityEngine;
using System.Collections.Generic;
using System.Linq;


public static class HandEvaluator {
    private static List<Meld> foundMelds = new();
    private static List<Meld> totalMelds = new();
    private static List<MahjongTile> foundPair = new();


    public static bool IsWinningHand(Player player) {
        foundMelds.Clear();
        totalMelds.Clear();
        foundPair.Clear();

        if (IsStandardHand(player)) {
            // Checks for standard hands go here
            if (PonsAndKans(player)) {
                Debug.Log("!! WINNING HAND FOUND (PonsAndKans) !!");
                return true;
            }
        }

        // Checks for non-standard hands go here
        if (Knitting(player)) {
            Debug.Log("!! WINNING HAND FOUND (Knitting) !!");
            return true;
        }

        return false;
    }
    
    private static bool IsStandardHand(Player player) {
        /* Standard Hand:
        Four melds plus a pair
        */

        int meldsNeeded = 4 - player.melds.Count;

        List<MahjongTile> tiles = new List<MahjongTile>(player.hand);
        // Order tiles by suit and number
        tiles = tiles
            .OrderBy(t => t.suit)
            .ThenBy(t => t.number)
            .ToList();

        for (int i = 0; i < tiles.Count; i++) {
            for (int j = i + 1; j < tiles.Count; j++) {
                // Only check matching pairs
                if (!tiles[i].IsSameTile(tiles[j]) || tiles[i].id == tiles[j].id) { continue; }

                MahjongTile tileA = tiles[i];
                MahjongTile tileB = tiles[j];

                // Remove found pair from remaining tiles
                List<MahjongTile> remaining = new List<MahjongTile>(tiles);
                remaining.Remove(tileA);
                remaining.Remove(tileB);

                List<Meld> currentFoundMelds = new();

                // Try to form melds from remaining tiles
                if (CanFormMelds(remaining, meldsNeeded, currentFoundMelds)) {
                    foundMelds = new List<Meld>(currentFoundMelds);
                    foundPair = new List<MahjongTile>{ tileA, tileB };
                    
                    // Combine players melds with found melds to get all melds
                    totalMelds = new List<Meld>(player.melds);
                    totalMelds.AddRange(foundMelds);

                    return true;
                }
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
    private static bool PonsAndKans(Player player) {
        /* Pons And Kans:
        Four Pons/Kans (one of which can be replaced by a Chi) plus a pair, all of a single suit (plus winds and dragons)
        */

        if (totalMelds.Count != 4) { return false; }        // If total melds isnt 4, hand cant be valid
        if (totalMelds.Count(m => m.type == CallType.Chi) > 1) { return false; }    // If hand has more than 1 Chi, hand cant be valid

        List<MahjongTile> allTiles = new();
        foreach (Meld meld in totalMelds) {
            allTiles.AddRange(meld.tiles);
        }

        allTiles.AddRange(foundPair);

        // Check all tiles are the same suit (plus winds and dragons)
        return IsSingleSuitExcludingHonours(allTiles);
    }

    private static bool Knitting(Player player) {
        /* Knitting:
        Four sets of three tiles, each set contains the same numbered tile from every suit (e.g. Three of Bamboo,
        Three of Dots and Three of Characters) plus a Pair, which follows the same rules as the sets, except it
        is missing any one of the three suits
        */

        if (totalMelds.Count > 0) { return false; }     // Knitting has no melds

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


    // -=-=- HELPERS -=-=-
    private static bool CanFormMelds(List<MahjongTile> tiles, int meldsNeeded, List<Meld> currentFoundMelds) {
        if (meldsNeeded == 0) { return tiles.Count == 0; }
        if (tiles.Count < 3) { return false; }      // Less than 3 tiles cant form a meld

        MahjongTile firstTile = tiles[0];

        // Try Pon (there will never be a Kan inside the players hand as that would get detected as a concealed Kan by GameManager)
        if (tiles.Count(t => t.IsSameTile(firstTile)) >= 3) {
            List<MahjongTile> ponTiles = new();
            List<MahjongTile> remaining = new List<MahjongTile>(tiles);

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

            currentFoundMelds.Add(new Meld(ponTiles, CallType.Pon));
            if (CanFormMelds(remaining, meldsNeeded - 1, currentFoundMelds)) { return true; }

            currentFoundMelds.RemoveAt(currentFoundMelds.Count - 1);
        }

        // Try Chi
        if (firstTile.IsNumbered()) {
            for (int i = 1; i < tiles.Count; i++) {
                if (!tiles[i].IsSequentialTo(firstTile, 1)) { continue; }

                for (int j = i + 1; j < tiles.Count; j++) {
                    if (!tiles[j].IsSequentialTo(firstTile, 2)) { continue; }

                    List<MahjongTile> chiTiles = new List<MahjongTile>{ firstTile, tiles[i], tiles[j] };
                    List<MahjongTile> remaining = new List<MahjongTile>(tiles);

                    remaining.Remove(firstTile);
                    remaining.Remove(tiles[i]);
                    remaining.Remove(tiles[j]);

                    currentFoundMelds.Add(new Meld(chiTiles, CallType.Chi));
                    if (CanFormMelds(remaining, meldsNeeded - 1, currentFoundMelds)) { return true; }

                    currentFoundMelds.RemoveAt(currentFoundMelds.Count - 1);
                }
            }
        }

        return false;
    }

    private static bool IsSingleSuitExcludingHonours(List<MahjongTile> tiles) {
        // Checks to see if tiles only contains tiles of the same suit (excluding winds and dragons)
        List<TileSuit> suits = tiles
            .Where(t => t.IsNumbered())
            .Select(t => t.suit)
            .Distinct()
            .ToList();
        
        return suits.Count <= 1;
    }

    private static bool IsSingleSuitIncludingHonours(List<MahjongTile> tiles) {
        // Checks to see if tiles only contains tiles of the same suit
        List<TileSuit> suits = tiles
            .Select(t => t.suit)
            .Distinct()
            .ToList();
        
        return suits.Count <= 1;
    }
}
