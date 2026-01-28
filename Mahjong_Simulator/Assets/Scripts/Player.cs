using UnityEngine;
using System.Collections.Generic;

public class Player {
    public int seat;
    public List<MahjongTile> hand = new();
    public List<MahjongTile> discards = new();

    public Player(int seat) { this.seat = seat; }
}
