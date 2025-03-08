using System;
using FistVR;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Linq;

namespace NGA {
    public class FGBank {
        public struct Config {
            public int playerBalance;
        }

        public int playerBalance { get; private set; }

        public FGBank() {
            playerBalance = 0;
        }

        public void InitFromConfig(Config config) {
            playerBalance = config.playerBalance;
        }

        public Config GetConfig() {
            return new Config { playerBalance = playerBalance };
        }

        public bool TryDecrementPlyBalance(int amount) {
            if (playerBalance >= amount) {
                playerBalance -= amount;
                return true;
            }
            return false;
        }

        public void ForceDecrementPlyBalance(int amount) {
            playerBalance -= amount;
        }

        public void IncrementPlyBalance(int amount) {
            playerBalance += amount;
        }
    }
} // namespace NGA