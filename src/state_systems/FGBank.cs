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
            public List<TransactionRecord> transactions;
        }

        [System.Serializable]
        public class TransactionRecord {
            public int amount;
            public string description;

            public TransactionRecord(int amount, string description) {
                this.amount = amount;
                this.description = description;
            }
        }

        public int playerBalance { get; private set; }
        public List<TransactionRecord> transactions { get; private set; }

        public FGBank() {
            playerBalance = 0;
            transactions = new List<TransactionRecord>();
        }

        public void InitFromConfig(Config config) {
            playerBalance = config.playerBalance;
            transactions = config.transactions ?? new List<TransactionRecord>();
        }

        public Config GetConfig() {
            return new Config { playerBalance = playerBalance, transactions = new List<TransactionRecord>(transactions) };
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

        public void ProcessTransaction(TransactionRecord transaction, bool forceDecrement) {
            if (transaction.amount < 0) {
                if (forceDecrement) {
                    ForceDecrementPlyBalance(-transaction.amount);
                } else {
                    if (!TryDecrementPlyBalance(-transaction.amount)) {
                        return;
                    }
                }
            } else {
                IncrementPlyBalance(transaction.amount);
            }
            transactions.Insert(0, transaction);
        }

        public string PrintPlyBankInfo(int maxTransactions = 10) {
            var info = $"<b>Current balance:</b> ${playerBalance:N0}\n\n";
            info += "<b>Recent transactions:</b>\n";
            int transactCount = 0;
            foreach (var transaction in transactions) {
                if (transactCount++ >= maxTransactions) {
                    break;
                }
                info += $"${transaction.amount:N0} {transaction.description}\n";
            }
            return info;
        }
    }
} // namespace NGA