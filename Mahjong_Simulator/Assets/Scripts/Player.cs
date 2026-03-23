using UnityEngine;
using System.Collections.Generic;

// Kong, Pung, Chow
public enum CallType { None, Kan, Pon, Chi };

public class Meld {
    public List<MahjongTile> tiles;
    public bool isConcealed;
    public CallType type;

    public Meld(List<MahjongTile> tiles, CallType type = CallType.None, bool isConcealed = false) {
        this.tiles = tiles;
        this.isConcealed = isConcealed;
        this.type = type;
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
