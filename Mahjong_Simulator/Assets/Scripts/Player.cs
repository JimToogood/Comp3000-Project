using UnityEngine;
using System.Collections.Generic;

// Kong, Pung, Chow
public enum CallType { Kan, Pon, Chi };

public class Player {
    public int seat;
    public List<MahjongTile> hand = new();
    public List<MahjongTile> discards = new();
    public List<List<MahjongTile>> melds = new();

    public CallType? pendingCall = null;
    public MahjongTile callTile = null;

    public Player(int seat) { this.seat = seat; }
}
