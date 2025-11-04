using System.Numerics;
using static System.Formats.Asn1.AsnWriter;

namespace tennis_score_win_probability
{
    internal class TennisWinningProbabilities
    {
        /// <summary>
        /// Calculate the probability of player1 winning the current game given the current score.
        /// Uses recursive calculation based on the probability of winning the next point.
        /// </summary>
        /// <param name="score1">Player1's current score (0-4, where 0=0, 1=15, 2=30, 3=40, 4=Ad)</param>
        /// <param name="score2">Player2's current score (0-4)</param>
        /// <param name="pointWonPct1">Probability that player1 wins any single point (0.0-1.0)</param>
        /// <returns>Probability that player1 wins the game (0.0-1.0)</returns>
        static float Player1GameWinningProbability(int score1, int score2, float pointWonPct1)
        {
            float prob = 0f;

            // Handle special case: if player1 wins a point at 40-Ad (score 3-4), 
            // the game returns to deuce (3-3), not 4-4
            if ((score1 == 4) && (score2 == 4))
            {
                score1 = 3; 
                score2 = 3;
            }

            // Deuce case (40-40): Use the mathematical formula for infinite series
            // P(win from deuce) = p^2 / (1 - 2p(1-p))
            // where p is the probability of winning a single point
            if ((score1 == 3) && (score2 == 3))
            {
                prob = (pointWonPct1 * pointWonPct1) / (1 - (2 * pointWonPct1 * (1 - pointWonPct1)));
                return prob;
            }

            // Player1 has won the game (scored 4+ points with lead of 2+)
            if ((score1 > 3) && ((score1 - score2) >= 2))
            {
                return 1.0f;
            }

            // Player1 has lost the game (opponent scored 4+ points with lead of 2+)
            if ((score2 > 3) && ((score2 - score1) >= 2))
            {
                return 0.0f;
            }

            // Recursive case: calculate probability as weighted sum of two scenarios:
            // 1. Player1 wins next point (probability = pointWonPct1)
            // 2. Player1 loses next point (probability = 1 - pointWonPct1)
            prob = (pointWonPct1 * Player1GameWinningProbability(score1 + 1, score2, pointWonPct1))
                + ((1 - pointWonPct1) * Player1GameWinningProbability(score1, score2 + 1, pointWonPct1));

            return prob;
        }

        /// <summary>
        /// Calculate the probability of player1 winning a tiebreak game.
        /// Accounts for alternating serves and different point-winning probabilities on serve vs return.
        /// </summary>
        /// <param name="tbScore1">Player1's current tiebreak score</param>
        /// <param name="tbScore2">Player2's current tiebreak score</param>
        /// <param name="player1ServicePointWinningProb">Probability player1 wins a point when serving</param>
        /// <param name="player1ReturnPointWinningProb">Probability player1 wins a point when returning</param>
        /// <param name="player1ServingFirst">True if player1 serves first in the tiebreak</param>
        /// <param name="tbTarget">Points needed to win tiebreak (default 7)</param>
        /// <returns>Probability that player1 wins the tiebreak (0.0-1.0)</returns>
        static public float tiebreakGameWinningProbability(int tbScore1, int tbScore2,
            float player1ServicePointWinningProb, float player1ReturnPointWinningProb, bool player1ServingFirst, int tbTarget = 7)
        {
            // Player1 wins: reached target score with 2+ point lead
            if ((tbScore1 >= tbTarget) && ((tbScore1 - tbScore2) >= 2))
            {
                return 1.0f;
            }

            // Player1 loses: opponent reached target score with 2+ point lead
            if ((tbScore2 >= tbTarget) && ((tbScore2 - tbScore1) >= 2))
            {
                return 0.0f;
            }

            // Recursion depth limit: if score reaches 16-16 (extremely rare), 
            // assume 50-50 probability to prevent stack overflow
            if ((tbScore1 == tbScore2) && (tbScore1 + tbScore2) > 30)
            {
                return 0.5f;
            }

            // Determine who serves the next point based on tiebreak serving rules:
            // - First server serves point 1
            // - Players alternate every 2 points thereafter
            // - After every 6 points total, players switch sides (but this doesn't affect serving order)
            bool player1ServingNextPoint = false;
            
            if ((tbScore1 == 0) && (tbScore2 == 0))
            {
                // First point: use the initial server
                player1ServingNextPoint = player1ServingFirst ? true : false;
            }
            else
            {
                // Subsequent points: determine server based on total points played
                int totalPoints = tbScore1 + tbScore2;

                if ((totalPoints % 2) == 0)
                {
                    // Even total points (0, 2, 4, ...): check if we're in first server's turn
                    if (((totalPoints / 2) % 2) == 0)
                    {
                        // Points 0-1, 4-5, 8-9, etc.: first server serves
                        player1ServingNextPoint = player1ServingFirst ? true : false;
                    }
                    else
                    {
                        // Points 2-3, 6-7, 10-11, etc.: second server serves
                        player1ServingNextPoint = player1ServingFirst ? false : true;
                    }
                }
                else
                {
                    // Odd total points (1, 3, 5, ...): second point of a pair
                    if ((((totalPoints + 1) / 2) % 2) == 0)
                    {
                        // Points 1, 5, 9, etc.: first server serves
                        player1ServingNextPoint = player1ServingFirst ? true : false;
                    }
                    else
                    {
                        // Points 3, 7, 11, etc.: second server serves
                        player1ServingNextPoint = player1ServingFirst ? false : true;
                    }
                }
            }

            // Calculate probability recursively based on who is serving next point
            if (player1ServingNextPoint)
            {
                // Player1 serving: use service point winning probability
                float tbProb = (player1ServicePointWinningProb * tiebreakGameWinningProbability(tbScore1 + 1, tbScore2,
                    player1ServicePointWinningProb, player1ReturnPointWinningProb, player1ServingFirst, tbTarget)) +
                    ((1 - player1ServicePointWinningProb) * tiebreakGameWinningProbability(tbScore1, tbScore2 + 1,
                    player1ServicePointWinningProb, player1ReturnPointWinningProb, player1ServingFirst, tbTarget));
                return tbProb;
            }
            else
            {
                // Player1 returning: use return point winning probability
                float tbProb = (player1ReturnPointWinningProb * tiebreakGameWinningProbability(tbScore1 + 1, tbScore2, 
                    player1ServicePointWinningProb, player1ReturnPointWinningProb, player1ServingFirst, tbTarget)) +
                    ((1 - player1ReturnPointWinningProb) * tiebreakGameWinningProbability(tbScore1, tbScore2 + 1,
                    player1ServicePointWinningProb, player1ReturnPointWinningProb, player1ServingFirst, tbTarget));
                return tbProb;
            }
        }

        /// <summary>
        /// Calculate the probability of player1 winning the current set given the current game score.
        /// Handles both regular sets (first to 6 games with 2+ lead) and tiebreak scenarios (at 6-6).
        /// </summary>
        /// <param name="score1">Player1's current game score in the set (0-7)</param>
        /// <param name="score2">Player2's current game score in the set (0-7)</param>
        /// <param name="player1ServicePointWinningProb">Probability player1 wins a point when serving</param>
        /// <param name="player1ReturnPointWinningProb">Probability player1 wins a point when returning</param>
        /// <param name="player1ServingFirst">True if player1 serves first in the set</param>
        /// <returns>Probability that player1 wins the set (0.0-1.0)</returns>
        static float Player1SetWinningProbability(int score1, int score2,
            float player1ServicePointWinningProb, float player1ReturnPointWinningProb, bool player1ServingFirst)
        {
            // Player1 wins: reached 6 games with 2+ game lead
            if ((score1 == 6) && ((score1 - score2) >= 2))
            {
                return 1.0f;
            }

            // Player1 wins: won tiebreak (7 games, opponent has 6 or fewer)
            if ((score1 == 7) && (score2 < 7))
            {
                return 1.0f;
            }

            // Player1 loses: opponent reached 6 games with 2+ game lead
            if ((score2 == 6) && ((score2 - score1) >= 2))
            {
                return 0.0f;
            }

            // Player1 loses: opponent won tiebreak
            if ((score2 == 7) && (score1 < 7))
            {
                return 0.0f;
            }

            // Determine what type of game is next and who serves
            bool tiebreakGameNext = false;
            bool player1ServiceGameNext = false;

            if ((score1 == 6) && (score2 == 6))
            {
                // At 6-6, a tiebreak is played
                tiebreakGameNext = true;
            }
            else
            {
                // Regular game: determine who serves based on total games played
                // Players alternate serving each game
                if (player1ServingFirst)
                {
                    // If player1 served first in the set:
                    // - Even total games (0, 2, 4, ...): player1 serves
                    // - Odd total games (1, 3, 5, ...): player2 serves
                    if ((score1 + score2) % 2 == 0)
                    {
                        player1ServiceGameNext = true;
                    }
                    else
                    {
                        player1ServiceGameNext = false;
                    }
                }
                else
                {
                    // If player2 served first in the set:
                    // - Even total games: player2 serves
                    // - Odd total games: player1 serves
                    if ((score1 + score2) % 2 == 0)
                    {
                        player1ServiceGameNext = false;
                    }
                    else
                    {
                        player1ServiceGameNext = true;
                    }
                }
            }

            // Calculate probability of player1 winning the next game
            float player1NextGameWinningProb = 0.0f;
            
            if(tiebreakGameNext)
            {
                // Tiebreak game: whoever served first in the set also serves first in the tiebreak
                player1NextGameWinningProb = tiebreakGameWinningProbability(0, 0, 
                    player1ServicePointWinningProb, player1ReturnPointWinningProb, player1ServingFirst);
            } 
            else
            {
                // Regular game: use appropriate point-winning probability based on who serves
                if (player1ServiceGameNext)
                {
                    // Player1 serving: use service game winning probability
                    player1NextGameWinningProb = Player1GameWinningProbability(0, 0, player1ServicePointWinningProb);
                }
                else
                {
                    // Player1 returning: use return game winning probability
                    player1NextGameWinningProb = Player1GameWinningProbability(0, 0, player1ReturnPointWinningProb);
                }
            }

            // Recursive calculation: weighted sum of winning/losing next game
            float probability = (player1NextGameWinningProb * Player1SetWinningProbability(score1 + 1, score2, 
                player1ServicePointWinningProb, player1ReturnPointWinningProb, player1ServingFirst)) +
                ((1 - player1NextGameWinningProb) * Player1SetWinningProbability(score1, score2 + 1, 
                player1ServicePointWinningProb, player1ReturnPointWinningProb, player1ServingFirst));

            return probability;
        }

        /// <summary>
        /// Convert numeric score (0-4) to traditional tennis scoring notation.
        /// </summary>
        /// <param name="simpleScore">Numeric score (0=0, 1=15, 2=30, 3=40, 4=Ad)</param>
        /// <returns>Traditional tennis score string</returns>
        static string ToTraditionalScore(int simpleScore)
        {
            switch (simpleScore)
            {
                case 0: return "0";
                case 1: return "15";
                case 2: return "30";
                case 3: return "40";
                case 4: return "Ad";
                default: break;
            }

            return "";
        }

        /// <summary>
        /// Calculate and print set winning probabilities for all possible game scores (0-6, 0-6)
        /// for both cases: player1 serving first and player2 serving first.
        /// </summary>
        /// <param name="player1ServicePointWinningProb">Probability player1 wins a point when serving</param>
        /// <param name="player1ReturnPointWinningProb">Probability player1 wins a point when returning</param>
        static public void CalcualteSetWinningProbabilities(float player1ServicePointWinningProb, 
            float player1ReturnPointWinningProb)
        {
            // Calculate for both scenarios: player1 serving first and player2 serving first
            for (int player1First = 0; player1First < 2; player1First++)
            {
                bool player1ServingFirst = player1First == 0 ? false : true;
                Console.WriteLine("Player1 serving first : " + player1ServingFirst);

                // Iterate through all possible game scores in a set
                for (int gameScore1 = 0; gameScore1 < 7; gameScore1++)
                {
                    for (int gameScore2 = 0; gameScore2 < 7; gameScore2++)
                    {
                        double player1SetWonProb = Player1SetWinningProbability(gameScore1, gameScore2,
                            player1ServicePointWinningProb, player1ReturnPointWinningProb, player1ServingFirst);
                        
                        // Output format: gameScore1,gameScore2,probability
                        Console.WriteLine(gameScore1 + "," + gameScore2 + "," + player1SetWonProb);
                    }
                }
            }
        }

        /// <summary>
        /// Calculate and print game winning probabilities for all possible point scores
        /// in a standard tennis game.
        /// </summary>
        /// <param name="pointWinPct1">Probability that player1 wins any single point</param>
        static public void CalculateGameWinningProbabilities(float pointWinPct1)
        {
            Console.WriteLine("Player1's probably of winning a point:" + pointWinPct1);

            float prob1 = 0.0f;

            // Calculate for all standard game scores (0-40, 0-40)
            for (int score1 = 0; score1 < 4; score1++)
            {
                for (int score2 = 0; score2 < 4; score2++)
                {
                    prob1 = Player1GameWinningProbability(score1, score2, pointWinPct1);

                    // Output format: score1,score2,probability (using traditional tennis notation)
                    Console.WriteLine(ToTraditionalScore(score1) + "," + ToTraditionalScore(score2) + "," + prob1);
                }
            }

            // Calculate special deuce-related scores
            // 40-Ad (player1 has advantage)
            prob1 = Player1GameWinningProbability(4, 3, pointWinPct1);
            Console.WriteLine(ToTraditionalScore(4) + "," + ToTraditionalScore(3) + "," + prob1);

            // Ad-40 (player2 has advantage)
            prob1 = Player1GameWinningProbability(3, 4, pointWinPct1);
            Console.WriteLine(ToTraditionalScore(3) + "," + ToTraditionalScore(4) + "," + prob1);
        }

        static void Main(string[] args)
        {
            // Set typical professional tennis point-winning probabilities
            // Player1 wins 59% of points on their serve
            float player1ServicePointWinningProb = 0.59f;
            
            // Player1 wins 44% of points on opponent's serve (return)
            float player1ReturnPointWinningProb = 0.44f;

            // Calculate and display game winning probabilities for service games
            CalculateGameWinningProbabilities(player1ServicePointWinningProb);
            
            // Uncomment to calculate set winning probabilities (computationally intensive)
            // CalcualteSetWinningProbabilities(player1ServicePointWinningProb, player1ReturnPointWinningProb);
        }
    }
}
