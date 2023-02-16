// Paula Sierra Luque
// Cynthia Tristán Álvarez

using System;
using System.Threading;

namespace FPII23_P1_Naves
{
    internal class Program
    {
        static Random rnd = new Random(); // un único generador de aleaotorios para todo el programa
        const bool DEBUG = true; // para sacar información adicional en el Render
        const int ANCHO = 20,
                  ALTO = 16,  // área de juego
                  MAX_BALAS = 5,
                  MAX_ENEMIGOS = 9;

        // Evitar Console.Clear en Windows
        // Errata en ColBalasTunel, se refiere a balas en vez de a nave obvs lol

        #region TIPOS
        struct Tunel
        {
            public int[] suelo, techo;
            public int ini;
        }

        struct Entidad
        {
            public int fil, col;
        }

        struct GrEntidades
        {
            public Entidad[] ent; // array de entidades
            public int num; // cantidad de entidades (o enemigos o balas) / la primera posicion libre
        }
        #endregion

        #region MÉTODOS
        static void IniciaTunel(out Tunel tunel)
        {
            Console.CursorVisible = false;

            // creamos arrays
            tunel.suelo = new int[ANCHO];
            tunel.techo = new int[ANCHO];

            // ** Bloque de iniciación manual
            //tunel.ini = 4;
            //tunel.techo = new int[] { 1, 1, 2, 1, 0 };
            //tunel.suelo = new int[] { 3, 3, 4, 3, 4 };
            // **

            // rellenamos posicion 0 como semilla para generar el resto
            tunel.techo[0] = 0;
            tunel.suelo[0] = ALTO - 1;

            // dejamos 0 como la última y avanzamos hasta dar la vuelta
            tunel.ini = 1;
            for (int i = 1; i < ANCHO; i++)
            {
                AvanzaTunel(ref tunel);
            }
            // al dar la vuelta y quedará tunel.ini=0    
        }

        static void AvanzaTunel(ref Tunel tunel)
        {
            // ultima pos del tunel: anterior a ini de manera circular
            int ult = (tunel.ini + ANCHO - 1) % ANCHO;

            // valores de suelo y techo en la última posicion
            int s = tunel.suelo[ult],
                t = tunel.techo[ult]; // incremento/decremento de suelo/techo

            // generamos nueva columna a partir de esta última
            int opt = rnd.Next(5); // obtenemos un entero de [0,4]
            if (opt == 0 && s < ALTO - 1) { s++; t++; }   // tunel baja y mantiene ancho
            else if (opt == 1 && t > 0) { s--; t--; }   // sube y mantiene ancho
            else if (opt == 2 && s - t > 7) { s--; t++; } // se estrecha (como mucho a 5)
            else if (opt == 3)
            {                    // se ensancha, si puede
                if (s < ALTO - 1) s++;
                if (t > 0) t--;
            } // con 4 sigue igual

            // guardamos nueva columna del tunel generada
            tunel.suelo[tunel.ini] = s;
            tunel.techo[tunel.ini] = t;

            // avanzamos la tunel.ini: siguiente en el array circular
            tunel.ini = (tunel.ini + 1) % ANCHO;
        }

        static char LeeInput()
        {
            char ch = ' ';
            if (Console.KeyAvailable)
            {
                string dir = Console.ReadKey(true).Key.ToString();
                if (dir == "A" || dir == "LeftArrow") ch = 'l';
                else if (dir == "D" || dir == "RightArrow") ch = 'r';
                else if (dir == "W" || dir == "UpArrow") ch = 'u';
                else if (dir == "S" || dir == "DownArrow") ch = 'd';
                else if (dir == "X" || dir == "Spacebar") ch = 'x'; // bala        
                else if (dir == "P") ch = 'p'; // pausa					
                else if (dir == "Q" || dir == "Escape") ch = 'q'; // salir
                while (Console.KeyAvailable) Console.ReadKey();
            }
            return ch;
        }

        static void RenderTunel(Tunel tunel)
        {
            Console.SetCursorPosition(0, 0);
            if (DEBUG)
            {
                for (int i = 0; i < ANCHO; i++)
                {
                    Console.Write(" " + tunel.techo[((tunel.ini + i) % ANCHO)] % 10);
                }
            }
            //DEBUG ↑ ↑ ↑ ↑ ↑ ↑ 

            for (int j = 0; j < ALTO; j++)
            {
                for (int i = 0; i < ANCHO; i++)
                {
                    if (j <= tunel.techo[(tunel.ini + i) % ANCHO] || j >= tunel.suelo[(tunel.ini + i) % ANCHO])
                        Console.BackgroundColor = ConsoleColor.DarkBlue;
                    else
                        Console.BackgroundColor = ConsoleColor.Black;
                    if (DEBUG)
                        Console.SetCursorPosition(i * 2, j + 1);
                    else
                        Console.SetCursorPosition(i * 2, j);
                    Console.Write("  ");
                }
            }

            //DEBUG ↓ ↓ ↓ ↓ ↓ ↓ ↓ 
            Console.BackgroundColor = ConsoleColor.Black;
            if (DEBUG)
            {
                Console.WriteLine();
                for (int i = 0; i < ANCHO; i++)
                {
                    Console.Write(" " + tunel.suelo[((tunel.ini + i) % ANCHO)] % 10);
                }
                Console.WriteLine("\nini: " + tunel.ini + " ");
            }
        }

        static void AñadeEntidad(Entidad ent, ref GrEntidades gr) 
        {
            gr.ent[gr.num] = ent;
            gr.num++;
        }

        static void EliminaEntidad(int i, ref GrEntidades gr) // Da errores en el penúltimo
        {
            gr.ent[i] = gr.ent[gr.num - 1];
            gr.num--;
        }

        static void AvanzaNave(char ch, ref Entidad nave)
        {
            switch (ch)
            {
                case 'l': // left
                    if (nave.col > 0)
                        nave.col--;
                    break;
                case 'r': // right
                    if (nave.col < ANCHO - 1)
                        nave.col++;
                    break;
                case 'u': // up
                    if (DEBUG && nave.fil > 1)
                        nave.fil--;
                    else if (!DEBUG && nave.fil > 0)
                        nave.fil--;
                    break;
                case 'd': // down
                    if (nave.fil < ALTO)
                        nave.fil++;
                    break;
                default:
                    break;
            }
        }

        static void Render(Tunel tunel, Entidad nave, GrEntidades enemigos)
        {
            RenderTunel(tunel);
            if (DEBUG)
            {
                Console.WriteLine("nave.col: " + nave.col + " ");
                Console.WriteLine("nave.fil: " + nave.fil + " ");
                Console.Write("enemigos.col: ");
                for(int i = 0; i < enemigos.num; i++)
                {
                    Console.Write((enemigos.ent[i].col) + ",");
                }
                Console.WriteLine();
                Console.Write("enemigos.fil: ");
                for (int i = 0; i < enemigos.num; i++)
                {
                    Console.Write((enemigos.ent[i].col) + ",");
                }
                Console.WriteLine();
            }
            Console.BackgroundColor = ConsoleColor.DarkMagenta;

            // NAVE
            Console.SetCursorPosition(nave.col * 2, nave.fil);
            Console.Write("=>");

            // ENEMIGOS
            for (int i = 0; i < enemigos.num; i++)
            {
                Console.SetCursorPosition(enemigos.ent[i].col * 2, enemigos.ent[i].fil); // ERROR EN EL PENÚLTIMO
                Console.Write("<>");
            }
            Console.BackgroundColor = ConsoleColor.Black;
        }

        static void GeneraEnemigo(ref GrEntidades enemigos, Tunel tunel)
        {
            if (enemigos.num < MAX_ENEMIGOS)
            {
                int chance = rnd.Next(0, 4);
                if (chance == 0)
                {
                    Entidad enemigo;
                    enemigo.col = ANCHO - 1;
                    enemigo.fil = rnd.Next(tunel.techo[(tunel.ini + ANCHO - 1) % ANCHO] + 1, tunel.suelo[(tunel.ini + ANCHO - 1) % ANCHO]);
                    AñadeEntidad(enemigo, ref enemigos);
                }
            }
        }

        static void AvanzaEnemigo(GrEntidades enemigos)
        {
            for (int i = 0; i < enemigos.num; i++)
            {
                enemigos.ent[i].col--;
                if (enemigos.ent[i].col <= 0) // Posible localización de error de borrado del penúltimo, hay que mirar igualmente
                    EliminaEntidad(i, ref enemigos);
            }
        }
        #endregion

        static void Main(string[] args)
        {
            Entidad nave;
            nave.col = ANCHO / 2;
            nave.fil = ALTO / 2;

            GrEntidades enemigos;
            enemigos.ent = new Entidad[MAX_ENEMIGOS];
            enemigos.num = 0;

            GrEntidades balas;
            balas.ent = new Entidad[MAX_BALAS];

            IniciaTunel(out Tunel tunel);
            Render(tunel, nave, enemigos);
            while (nave.fil >= 0)
            {
                char ch = LeeInput();
                AvanzaTunel(ref tunel);
                GeneraEnemigo(ref enemigos, tunel);
                AvanzaEnemigo(enemigos);
                AvanzaNave(ch, ref nave);
                Render(tunel, nave, enemigos);
                Thread.Sleep(100);
            }
        }
    }
}
