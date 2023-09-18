using System.Numerics;
using static System.Formats.Asn1.AsnWriter;

namespace tennis_score_win_probability
{
    internal class TennisWinningProbabilities
    {
        // Calculate the probability of player1 winning the game 
        static float Player1GameWinningProbability(int score1, int score2, float pointWonPct1)
        {
            float prob = 0f;

            if ((score1 == 4) && (score2 == 4))
            {
                // If player1 wins a point at 40-Ad (3-4), the score goes to 3-3 (deuce), not 4-4.
                score1 = 3; score2 = 3;
            }

            if ((score1 == 3) && (score2 == 3))
            {
                // Deuce
                prob = (pointWonPct1 * pointWonPct1) / (1 - (2 * pointWonPct1 * (1 - pointWonPct1)));
                return prob;
            }

            if ((score1 > 3) && ((score1 - score2) >= 2))
            {
                // Player1 wins the game
                return 1.0f;
            }

            if ((score2 > 3) && ((score2 - score1) >= 2))
            {
                // Player1 loses the game
                return 0.0f;
            }

            prob = (pointWonPct1 * Player1GameWinningProbability(score1 + 1, score2, pointWonPct1))
                + ((1 - pointWonPct1) * Player1GameWinningProbability(score1, score2 + 1, pointWonPct1));

            return prob;
        }

        static public float tiebreakGameWinningProbability(int tbScore1, int tbScore2,
            float player1ServicePointWinningProb, float player1ReturnPointWinningProb, bool player1ServingFirst, int tbTarget = 7)
        {
            if ((tbScore1 >= tbTarget) && ((tbScore1 - tbScore2) >= 2))
            {
                // player1 wins the tiebreak
                return 1.0f;
            }

            if ((tbScore2 >= tbTarget) && ((tbScore2 - tbScore1) >= 2))
            {
                // player1 loses the tiebreak
                return 0.0f;
            }

            if ((tbScore1 == tbScore2) && (tbScore1 + tbScore2) > 30)
            {
                // Stop the recursion if the score reaches 16-16, which is quite rare.
                return 0.5f;
            }

            // Figuring out who is serving the next point
            bool player1ServingNextPoint = false;
            if ((tbScore1 == 0) && (tbScore2 == 0))
            {
                player1ServingNextPoint = player1ServingFirst ? true : false;
            }
            else
            {
                // The sum of scores is larger than 0.

                if (((tbScore1 + tbScore2) % 2) == 0)
                {
                    // The sum of scores is even.

                    if ((((tbScore1 + tbScore2) / 2) % 2) == 0)
                    {
                        // if score_sum/2 is even, the next server is the first server. 
                        player1ServingNextPoint = player1ServingFirst ? true : false;
                    }
                    else
                    {
                        // if score_sum/2 is odd, the next server the the second server.
                        player1ServingNextPoint = player1ServingFirst ? false : true;
                    }
                }
                else
                {
                    // The sum of scores is odd.
                    if ((((tbScore1 + tbScore2 + 1) / 2) % 2) == 0)
                    {
                        // if (score_sum + 1)/2 is even, the next server is the first server. 
                        player1ServingNextPoint = player1ServingFirst ? true : false;
                    }
                    else
                    {
                        // if (score_sum + 1)/2 is odd, the next server the the second server.
                        player1ServingNextPoint = player1ServingFirst ? false : true;
                    }
                }
            }

            if (player1ServingNextPoint)
            {
                float tbProb = (player1ServicePointWinningProb * tiebreakGameWinningProbability(tbScore1 + 1, tbScore2,
                    player1ServicePointWinningProb, player1ReturnPointWinningProb, player1ServingFirst, tbTarget)) +
                    ((1 - player1ServicePointWinningProb) * tiebreakGameWinningProbability(tbScore1, tbScore2 + 1,
                    player1ServicePointWinningProb, player1ReturnPointWinningProb, player1ServingFirst, tbTarget));
                return tbProb;
            }
            else
            {
                float tbProb = (player1ReturnPointWinningProb * tiebreakGameWinningProbability(tbScore1 + 1, tbScore2, 
                    player1ServicePointWinningProb, player1ReturnPointWinningProb, player1ServingFirst, tbTarget)) +
                    ((1 - player1ReturnPointWinningProb) * tiebreakGameWinningProbability(tbScore1, tbScore2 + 1,
                    player1ServicePointWinningProb, player1ReturnPointWinningProb, player1ServingFirst, tbTarget));
                return tbProb;

            }
        }

        static float Player1SetWinningProbability(int score1, int score2,
            float player1ServicePointWinningProb, float player1ReturnPointWinningProb, bool player1ServingFirst)
        {
            if ((score1 == 6) && ((score1 - score2) >= 2))
            {
                // player 1 wins
                return 1.0f;
            }

            if ((score1 == 7) && (score2 < 7))
            {
                // player 1 wins
                return 1.0f;
            }

            if ((score2 == 6) && ((score2 - score1) >= 2))
            {
                // player 1 loses
                return 0.0f;
            }

            if ((score2 == 7) && (score1 < 7))
            {
                // player 1 loses
                return 0.0f;
            }

            bool tiebreakGameNext = false;
            bool player1ServiceGameNext = false;

            if ((score1 == 6) && (score2 == 6))
            {
                tiebreakGameNext = true;
            }
            else
            {
                if (player1ServingFirst)
                {
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

            float player1NextGameWinningProb = 0.0f;
            if(tiebreakGameNext)
            {
                // If it's a tiebreak game next, use the tiebreak game probability

                // Whoever serves first in the set also serves first in the tiebreak.
                player1NextGameWinningProb = tiebreakGameWinningProbability(0, 0, player1ServicePointWinningProb, player1ReturnPointWinningProb, player1ServingFirst);
            } else
            {
                // Not a tiebreak game next
                if (player1ServiceGameNext)
                {
                    player1NextGameWinningProb = Player1GameWinningProbability(0, 0, player1ServicePointWinningProb);
                }
                else
                {
                    player1NextGameWinningProb = Player1GameWinningProbability(0, 0, player1ReturnPointWinningProb);
                }
            }

            float probability = (player1NextGameWinningProb * Player1SetWinningProbability(score1 + 1, score2, player1ServicePointWinningProb, player1ReturnPointWinningProb, player1ServingFirst)) +
                ((1 - player1NextGameWinningProb) * Player1SetWinningProbability(score1, score2 + 1, player1ServicePointWinningProb, player1ReturnPointWinningProb, player1ServingFirst));

            return probability;
        }

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

        static public void CalcualteSetWinningProbabilities(float player1ServicePointWinningProb, 
            float player1ReturnPointWinningProb)
        {
            // Calculating set winning probability

            for (int player1First = 0; player1First < 2; player1First++)
            {
                bool player1ServingFirst = player1First == 0 ? false : true;
                Console.WriteLine("Player1 serving first : " + player1ServingFirst);

                for (int gameScore1 = 0; gameScore1 < 7; gameScore1++)
                {
                    for (int gameScore2 = 0; gameScore2 < 7; gameScore2++)
                    {
                        double player1SetWonProb = Player1SetWinningProbability(gameScore1, gameScore2,
                            player1ServicePointWinningProb, player1ReturnPointWinningProb, player1ServingFirst);
                        Console.WriteLine(gameScore1 + "," + gameScore2 + "," + player1SetWonProb);
                    }
                }
            }
        }

        static public void CalculateGameWinningProbabilities(float pointWinPct1)
        {
            Console.WriteLine("Player1's probably of winning a point:" + pointWinPct1);

            float prob1 = 0.0f;

            for (int score1 = 0; score1 < 4; score1++)
            {
                for (int score2 = 0; score2 < 4; score2++)
                {
                    prob1 = Player1GameWinningProbability(score1, score2, pointWinPct1);

                    Console.WriteLine(ToTraditionalScore(score1) + "," + ToTraditionalScore(score2) + "," + prob1);
                }
            }

            prob1 = Player1GameWinningProbability(4, 3, pointWinPct1);
            Console.WriteLine(ToTraditionalScore(4) + "," + ToTraditionalScore(3) + "," + prob1);

            prob1 = Player1GameWinningProbability(3, 4, pointWinPct1);
            Console.WriteLine(ToTraditionalScore(3) + "," + ToTraditionalScore(4) + "," + prob1);
        }

        static void Main(string[] args)
        {
            float player1ServicePointWinningProb = 0.59f;
            float player1ReturnPointWinningProb = 0.44f;

            CalculateGameWinningProbabilities(player1ServicePointWinningProb);
            // CalcualteSetWinningProbabilities(player1ServicePointWinningProb, player1ReturnPointWinningProb);
        }
    }
}