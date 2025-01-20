using UnityEngine;

namespace GamePlay
{
    // Enum to represent the type of obstacle
    public enum ObstacleType
    {
        Player, // reduce 30% damage
        Wood, // reduce 50% damage
        Concrete, // reduce 70% damage
        Iron, // reduce 100% damage
    }
    public class Obstacle : MonoBehaviour
    {
        public ObstacleType obstacleType; // Type of the obstacle
    }
}
