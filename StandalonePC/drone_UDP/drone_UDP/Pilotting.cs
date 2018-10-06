using System;

namespace drone_UDP
{
    public static class Pilotting
    {
        /// <summary>
        /// Reads the user input and executes the command.
        /// </summary>
        /// <param name="bebop">An instance of <see cref="BebopCommand"/>.</param>
        /// <returns>false if quiting.</returns>
        public static bool ExecuteCommandOnUserInput(BebopCommand bebop)
        {
            var input = Console.ReadLine();
            switch (input)
            {
                //takeoff
                case "t":
                    bebop.takeoff();
                    break;
                //landing
                //moving command: -100% ~ 100%
                case "l":
                    bebop.landing();
                    break;
                //left
                case "a":
                    bebop.move(1, -10, 0, 0, 0);
                    break;
                //right
                case "d":
                    bebop.move(1, 10, 0, 0, 0);
                    break;
                //forward
                case "w":
                    bebop.move(1, 0, 10, 0, 0);
                    break;
                //backward
                case "s":
                    bebop.move(1, 0, -10, 0, 0);
                    break;
                //turn left
                case "h":
                    bebop.move(0, 0, 0, -10, 0);
                    break;
                //turn right
                case "k":
                    bebop.move(0, 0, 0, 10, 0);
                    break;
                //up
                case "u":
                    bebop.move(0, 0, 0, 0, 10);
                    break;
                //down
                case "j":
                    bebop.move(0, 0, 0, 0, -10);
                    break;
                //pause
                case "p":
                    bebop.move(0, 0, 0, 0, 0);
                    break;
                case "v":
                    bebop.videoEnable(); //enable RTP/.H264 videostreaming
                    break;
                //quit
                case "q":
                    bebop.cancleAllTask();
                    return false;
                default:
                    Console.WriteLine("Invalid command, try again.");
                    break;
            }

            return true;
        }
    }
}