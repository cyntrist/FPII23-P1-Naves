﻿// Paula Sierra Luque
// Cynthia Tristán Álvarez

using System;
using System.Threading;
using System.Media;
using WMPLib;

namespace FPII23_P1_Naves
{
    internal class Program
    {
        static readonly Random rnd = new Random(); // un único generador de aleaotorios para todo el programa
        static readonly WindowsMediaPlayer ostPlayer = new WindowsMediaPlayer();
        static readonly SoundPlayer sfxPlayer = new SoundPlayer();
        const bool DEBUG = false; // para sacar información adicional en el Render
        const int ANCHO = 20,
                  ALTO = 16,  // área de juego
                  MAX_BALAS = 5,
                  MAX_ENEMIGOS = 9;
        const string SHOOT_SOUND = @"shoot.wav",
                     HIT_SOUND = @"hit.wav";

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
                    Console.Write(" " + tunel.techo[((tunel.ini + i) % ANCHO)] % 10);
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
                    Console.SetCursorPosition(i * 2, j + Convert.ToInt16(DEBUG));
                    Console.Write("  ");
                }
            }

            //DEBUG ↓ ↓ ↓ ↓ ↓ ↓ ↓ 
            Console.BackgroundColor = ConsoleColor.Black;
            if (DEBUG)
            {
                Console.WriteLine();
                for (int i = 0; i < ANCHO; i++)
                    Console.Write(" " + tunel.suelo[((tunel.ini + i) % ANCHO)] % 10);
                Console.WriteLine("\nini: " + tunel.ini + " ");
            }
        }

        static void AñadeEntidad(Entidad ent, ref GrEntidades gr)
        {
            gr.ent[gr.num] = ent;
            gr.num++;
        }

        static void EliminaEntidad(int i, ref GrEntidades gr) // me cago en la puta de oros DIOSSSSSSSSSSSSS
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
                    if (nave.fil > 0)
                        nave.fil--;
                    break;
                case 'd': // down
                    if (nave.fil < ALTO - 1)
                        nave.fil++;
                    break;
                default:
                    break;
            }
        }

        static void Render(Tunel tunel, Entidad nave, GrEntidades enemigos, GrEntidades balas, GrEntidades colisiones)
        {
            RenderTunel(tunel);
            if (DEBUG)
            {
                Console.WriteLine("nave.col: " + nave.col + ", nave.fil: " + nave.fil + "  ");                                  
                Console.Write("enemigos.col: "); for (int i = 0; i < enemigos.num; i++) Console.Write(enemigos.ent[i].col + "  ");
                Console.Write("\nenemigos.fil: "); for (int i = 0; i < enemigos.num; i++) Console.Write(enemigos.ent[i].fil + "  ");
                Console.WriteLine("\nenemigos.num: " + enemigos.num);
                Console.WriteLine("balas.num: " + balas.num);                                       
            }

            Console.BackgroundColor = ConsoleColor.DarkMagenta;
            // NAVE
            if (nave.fil >= 0) // hay que ver como optimizar este if, pero como el enunciado dice de usar nave.fil 
                               // como condicion para parar el juego pues lol jijjijiji
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.SetCursorPosition(nave.col * 2, nave.fil + Convert.ToInt16(DEBUG));
                Console.Write("=>");
            }

            // ENEMIGOS
            Console.ForegroundColor = ConsoleColor.Yellow;
            for (int i = 0; i < enemigos.num; i++)
            {
                if (enemigos.ent[i].col >= 0)
                {
                    Console.SetCursorPosition(enemigos.ent[i].col * 2, enemigos.ent[i].fil + Convert.ToInt16(DEBUG));
                    Console.Write("<>");
                }
            }

            // BALAS
            Console.ForegroundColor = ConsoleColor.Magenta;
            for (int i = 0; i < balas.num; i++)
            {
                Console.SetCursorPosition(balas.ent[i].col * 2, balas.ent[i].fil + Convert.ToInt16(DEBUG));
                Console.Write("--");
            }

            // COLISIONES
            Console.ForegroundColor = ConsoleColor.Red;
            for (int i = 0; i < colisiones.num; i++)
            {
                Console.SetCursorPosition(colisiones.ent[i].col * 2, colisiones.ent[i].fil + Convert.ToInt16(DEBUG));
                Console.Write("**");
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Black;
        }

        static void GeneraEnemigo(ref GrEntidades enemigos, Tunel tunel)
        {
            if (enemigos.num < MAX_ENEMIGOS)
            {
                int chance;
                if (DEBUG) chance = 0;
                else chance = rnd.Next(0, 4);

                if (chance == 0)
                {
                    int ind = (tunel.ini + ANCHO - 1) % ANCHO;
                    Entidad enemigo;
                    enemigo.col = ANCHO - 1;
                    enemigo.fil = rnd.Next(tunel.techo[ind] + 1, tunel.suelo[ind] - 1);
                    AñadeEntidad(enemigo, ref enemigos);
                }
            }
        }

        static void AvanzaEnemigo(ref GrEntidades enemigos)
        {
            for (int i = 0; i < enemigos.num; i++)
            {
                enemigos.ent[i].col--;
                if (enemigos.ent[i].col < 0)
                    EliminaEntidad(i, ref enemigos);
            }
        }

        static void GeneraBala(ref GrEntidades balas, Entidad nave)
        {
            if (balas.num < MAX_BALAS && nave.col < ANCHO - 1)
            {
                Entidad bala;
                bala.col = nave.col;
                bala.fil = nave.fil;
                AñadeEntidad(bala, ref balas);
                Sonido(sfxPlayer, SHOOT_SOUND);
            }
        }

        static void AvanzaBalas(ref GrEntidades balas)
        {
            for (int i = 0; i < balas.num; i++)
            {
                balas.ent[i].col++;
                if (balas.ent[i].col >= ANCHO)
                    EliminaEntidad(i, ref balas);
            }
        }

        static void ColNaveTunel(Tunel tunel, ref Entidad nave, ref GrEntidades colisiones)
        {
            int ind = (tunel.ini + nave.col) % ANCHO; 
            if (nave.fil <= tunel.techo[ind] || nave.fil >= tunel.suelo[ind])
            {
                Entidad colision;
                colision.fil = nave.fil;
                colision.col = nave.col;
                AñadeEntidad(colision, ref colisiones);
                nave.fil = -1;
                Sonido(sfxPlayer, HIT_SOUND);
            }
        }

        static void ColBalasTunel(Tunel tunel, ref GrEntidades balas, ref GrEntidades colisiones)
        { // IMPORTANTE, QUE LAS BALAS DESTRUYA UN HUECO DEL TUNEL DONDE CHOCAN_: es lo único que queda creo
            for (int i = 0; i < balas.num; i++)
            {
                int ind = (tunel.ini + balas.ent[i].col) % ANCHO;
                if (balas.ent[i].fil <= tunel.techo[ind] || balas.ent[i].fil >= tunel.suelo[ind])
                {
                    Entidad colision;
                    colision.fil = balas.ent[i].fil;
                    colision.col = balas.ent[i].col;
                    AñadeEntidad(colision, ref colisiones);
                    EliminaEntidad(i, ref balas);
                    Sonido(sfxPlayer, HIT_SOUND);
                }
            }
        }

        static void ColNaveEnemigos(ref Entidad nave, ref GrEntidades enemigos, ref GrEntidades colisiones)
        {
            for (int i = 0; i < enemigos.num; i++)
            {
                if (nave.fil == enemigos.ent[i].fil && nave.col == enemigos.ent[i].col)
                {
                    Entidad colision;
                    colision.fil = enemigos.ent[i].fil;
                    colision.col = enemigos.ent[i].col;
                    AñadeEntidad(colision, ref colisiones);
                    EliminaEntidad(i, ref enemigos);
                    nave.fil = -1;
                    Sonido(sfxPlayer, HIT_SOUND);
                }
            }
        }

        static void ColBalasEnemigos(ref GrEntidades balas, ref GrEntidades enemigos, ref GrEntidades colisiones)
        {
            for (int i = 0; i < enemigos.num; i++)
            {
                for (int j = 0; j < balas.num; j++)
                {
                    if (enemigos.ent[i].fil == balas.ent[j].fil && enemigos.ent[i].col == balas.ent[j].col)
                    {
                        Entidad colision; 
                        colision.fil = enemigos.ent[i].fil; 
                        colision.col = enemigos.ent[i].col;
                        AñadeEntidad(colision, ref colisiones);
                        EliminaEntidad(i, ref enemigos);
                        EliminaEntidad(j, ref balas);
                        Sonido(sfxPlayer, HIT_SOUND);
                    }
                }
            }
        }

        static void Colisiones(ref Tunel tunel, ref Entidad nave, ref GrEntidades balas, ref GrEntidades enemigos, ref GrEntidades colisiones)
        {
            ColNaveTunel(tunel, ref nave, ref colisiones);
            ColBalasTunel(tunel, ref balas, ref colisiones);
            ColNaveEnemigos(ref nave, ref enemigos, ref colisiones);
            ColBalasEnemigos(ref balas, ref enemigos, ref colisiones);
        }

        static void Sonido(SoundPlayer player, string sonido)
        {
            player.SoundLocation = sonido;
            player.Play();
        }
        #endregion

        static void Main()
        {
            Entidad nave;
            nave.col = ANCHO / 2;
            nave.fil = ALTO / 2;

            GrEntidades enemigos;
            enemigos.ent = new Entidad[MAX_ENEMIGOS];
            enemigos.num = 0;

            GrEntidades balas;
            balas.ent = new Entidad[MAX_BALAS];
            balas.num = 0;

            GrEntidades colisiones;
            colisiones.ent = new Entidad[ANCHO * ALTO];
            colisiones.num = 0;

            IniciaTunel(out Tunel tunel);
            Render(tunel, nave, enemigos, balas, colisiones);
            ostPlayer.URL = @"ost.wav";
            while (nave.fil >= 0) // bucle principal
            {
                char ch = LeeInput();
                AvanzaTunel(ref tunel);
                GeneraEnemigo(ref enemigos, tunel);
                AvanzaEnemigo(ref enemigos);
                Colisiones(ref tunel, ref nave, ref balas, ref enemigos, ref colisiones);
                if (nave.fil >= 0) // segun enunciado
                {
                    AvanzaNave(ch, ref nave);
                    if (ch == 'x') GeneraBala(ref balas, nave);
                    AvanzaBalas(ref balas);
                    Colisiones(ref tunel, ref nave, ref balas, ref enemigos, ref colisiones);
                }
                Render(tunel, nave, enemigos, balas, colisiones);
                Thread.Sleep(200);
                for (int i = 0; i < colisiones.num; i++) // Elimina colisiones
                {
                    EliminaEntidad(i, ref colisiones);
                }
            }
            ostPlayer.close();
            Console.Clear();
            Console.WriteLine("El juego ha finalizado.");
            while (true) ;
        }
    }
}
