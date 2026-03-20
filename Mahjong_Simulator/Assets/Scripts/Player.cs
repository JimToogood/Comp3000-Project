using UnityEngine;
using System.Collections.Generic;

// Kong, Pung, Chow
public enum CallType { Kan, Pon, Chi };

public class Meld {
    public List<MahjongTile> tiles;
    public bool isConcealed;

    public Meld(List<MahjongTile> tiles, bool isConcealed = false) {
        this.tiles = tiles;
        this.isConcealed = isConcealed;
    }
}

public class Player {
    public int seat;
    public List<MahjongTile> hand = new();
    public List<MahjongTile> discards = new();
    public List<Meld> melds = new();

    public CallType? pendingCall = null;
    public MahjongTile callTile = null;

    public Player(int seat) { this.seat = seat; }
}
