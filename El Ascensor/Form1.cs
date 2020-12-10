using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace El_Ascensor
{
    public partial class ElAscensor : Form
    {
        //Simulacion en curso
        bool Simulacion = false;

        //BASE DE CONOCIMIENTOS
        private int PisoActualD = 1; //Dato del piso actual
        private List<Peticion> paradas = new List<Peticion>();

        /* Paradas: son todas las paradas que se han introducido, tanto la de los pisos como los botones de los ascensores, por ejemplo:
         * 5⇵: Alguien ha pulsado el boton 5 en el ascensor
         * 5↓: En el piso 5, alguien ha pulsado el boton de bajar
         * 5↑: En el piso 5, alguien ha pulsado el boton de subir
         */

        private bool Sube_baja = true; //Indica si el ascensor sube (true) o baja (false)
        private bool EnMarcha = false; //Indica si el ascensor se esta moviendo (true, en este caso evaluar sube_baja) o no (false, no evaluar sube_baja)
        //FIN BASE DE CONOCIMINETOS

        private Hashtable paradasY = new Hashtable(); //Posicion Y de los pisos para saber donde pararse en cada parada, por ejemplo, piso 1 y:596px
        private Thread t; //El ascensor es un hilo independiente, si no lo fuera al pulsar los botones, estos se quedan esperando a que el ascensor acabe

        private bool PT; //Indica si la puerta esta cerrada (true) o abierta (false)
        public ElAscensor()
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            PisoActual.Text = PisoActualD.ToString(); //Imprimimos por pantalla el piso actual

            // Introducimos la informacion de las Ys en cada piso
            paradasY.Add(1, Constants.ypiso1);
            paradasY.Add(2, Constants.ypiso2);
            paradasY.Add(3, Constants.ypiso3);
            paradasY.Add(4, Constants.ypiso4);
            paradasY.Add(5, Constants.ypiso5);
            paradasY.Add(6, Constants.ypiso6);

            //Creamos thread del ascensor para que se pueda mover libremente
            t = new Thread(new ThreadStart(MoverAscensor));
            t.Start();

            //Este bool sirve para que desde el thread se pueda llamar a objetos creados por el programa principal
            CheckForIllegalCrossThreadCalls = false;

            //Al cerrar el form cerramos el thread
            this.FormClosed += Form1_FormClosed;
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            //Cerramos el thread
            t.Abort();
        }

        //Funcion que se encarga de mirar continuamente la lista de paradas, si hay alguna parada, escogerá la que se adapte a sus necesidades
        //y se moverá hasta la Y que corresponda a esa parada
        private void MoverAscensor()
        {
            //El ascensor siempre esta esperando peticiones (siempre esta encendido)
            while (true)
            {
                while (Simulacion)
                {
                    //Si hay paradas se pone en marcha
                    while (HihaPet())
                    {

                        EnMarcha = true;

                        //Selecciona el psio en base a la base de conocimientos
                        Peticion peticion = SeleccionarPiso();

                        //Mira hasta donde tiene que moverse
                        int objetivoY = (int)paradasY[peticion.Piso];

                        //Indicacion de cuanto falta para llegar al piso
                        ProgressBar(objetivoY);

                        //Si se ha pulsado el piso actual no hacer nada
                        if (AscensorImagen.Location.Y != objetivoY)
                        {
                            //ir subiendo o bajando hasta la Y
                            while (AscensorImagen.Location.Y != objetivoY)
                            {
                                if (AscensorImagen.Location.Y < objetivoY)
                                {
                                    //Indicamos si sube o baja y actualizamos las luces de subir o bajar
                                    Sube_baja = false;
                                    ActualizarSube_Baja();
                                    AscensorImagen.Location = new Point(AscensorImagen.Location.X, AscensorImagen.Location.Y + 1);
                                }
                                else
                                {
                                    Sube_baja = true;
                                    ActualizarSube_Baja();
                                    AscensorImagen.Location = new Point(AscensorImagen.Location.X, AscensorImagen.Location.Y - 1);
                                }
                                progressBar.PerformStep();
                                //Esperar un tiempo x para que no suba muy rapido
                                System.Threading.Thread.Sleep(10);
                            }
                            ActualizarSube_Baja();
                        }
                        //Llega al piso y actualiza los pisos pendientes y se abre y cierra la puerta
                        LlegadaAPiso(peticion);
                        ActualizarPisosAVisitar();
                    }
                    EnMarcha = false;
                    ActualizarSube_Baja();
                    ProgressBar(0);
                }
            }
        }

        private bool HihaPet()
        {
            return paradas.Count != 0;
        }

        //En esta funcion se aplica toda la logica de seleccionar el piso al subir o bajar, dependiendo del piso donde estas...
        private Peticion SeleccionarPiso()
        {
            Peticion objetivo = paradas[0]; //de momento escoge el primer piso

            //Subir un piso------------------------------------------------------
            /*
             * El ascensor siempre espera a cerrar la puerta para tomar una decisión
             * Miramos si no hay solicitudes de parada ni de llamada para el piso actual
             * Si no existe una solicitud en un piso inferior mientras se esta bajando
             * Si existe alguna solicitud de parada y/o llamada en el piso superior
             */

            /*
             * PujBai ˄ PT ˄ ¬Sol_atu_pis_act ˄ Sol_atu_pis_sup → PujarUnPis
             * PujBai ˄ PT ˄ ¬Sol_cri_pis_act ˄ Sol_cri_pis_sup →  PujarUnPis
             * ¬PujBai ˄ PT ˄ ¬Sol_atu_pis_act ˄ Sol_atu_pis_sup → PujarUnPis
             * ¬PujBai ˄ PT ˄ ¬Sol_cri_pis_act ˄ Sol_cri_pis_sup → PujarUnPis
             */

            if (!SolicitudLlamada(PisoActualD) && !SolicitudParada(PisoActualD))
            {
                objetivo = SolicitudPisoInferior(PisoActualD);

                if (objetivo == null || Sube_baja)
                {

                    objetivo = SolicitudPisoSuperior(PisoActualD);
                    if (objetivo != null)
                    {
                        PisoObjetivo.Text = objetivo.Piso.ToString();
                        PisoObjetivo.Refresh();
                        return objetivo;
                    }

                }
            }

            //Bajar un piso-------------------------------------------------------
            /*
             * El ascensor siempre espera a cerrar la puerta para tomar una decisión
             * Miramos si no hay solicitudes de parada ni de llamada para el piso actual
             * Si no existe una solicitud en un piso superior mientras se esta subiendo
             * Si existe alguna solicitud de parada y/o llamada en el piso inferior
             */

            /*
             * ¬PujBai ˄ PT ˄ ¬Sol_atu_pis_act ˄ Sol_atu_pis_inf → BaixarUnPis
             * ¬PujBai ˄ PT ˄ ¬Sol_cri_pis_act ˄ Sol_cri_pis_inf →  BaixarUnPis
             * PujBai ˄ PT ˄ ¬Sol_atu_pis_act ˄ Sol_atu_pis_inf → BaixarUnPis 
             * PujBai ˄ PT ˄ ¬Sol_cri_pis_act ˄ Sol_cri_pis_inf → BaixarUnPis 
             */

            if (!SolicitudLlamada(PisoActualD) && !SolicitudParada(PisoActualD))
            {
                objetivo = SolicitudPisoSuperior(PisoActualD);

                if (objetivo == null || !Sube_baja)
                {

                    objetivo = SolicitudPisoInferior(PisoActualD);
                    if (objetivo != null)
                    {
                        PisoObjetivo.Text = objetivo.Piso.ToString();
                        PisoObjetivo.Refresh();
                        return objetivo;
                    }

                }
            }

            /*
             * Sol_cri_pis_act ˄ Sol_atu_pis_act → Actualitzar_pis_objectiu 
             */
            if (SolicitudLlamada(PisoActualD) && SolicitudParada(PisoActualD))
            {
                objetivo = paradas.ToList().Where(x => x.Piso == PisoActualD).FirstOrDefault();
            }


            if (objetivo == null)
            {
                objetivo = paradas[0]; //escoge el primer piso si no hay ninguno seleccionado o error
            }

            PisoObjetivo.Text = objetivo.Piso.ToString();
            PisoObjetivo.Refresh();
            return objetivo;
        }

        private void ActualizarSube_Baja()
        {
            if (!EnMarcha)
            {
                Sube.BackColor = Color.FromArgb(255, 227, 227, 227);
                Baja.BackColor = Color.FromArgb(255, 227, 227, 227);
            }
            else
            {
                if (Sube_baja)
                {
                    Sube.BackColor = Color.Green;
                    Baja.BackColor = Color.FromArgb(255, 227, 227, 227);
                }
                else
                {
                    Sube.BackColor = Color.FromArgb(255, 227, 227, 227);
                    Baja.BackColor = Color.Green;
                }
            }
            Sube.Refresh();
            Baja.Refresh();
        }

        //Imprime por pantalla los pisos
        private void ActualizarPisosAVisitar()
        {
            PisosAVisitar.Text = "{ ";
            foreach (Peticion piso in paradas)
            {
                PisosAVisitar.Text += piso.Piso.ToString();
                if (piso.Panel)
                {
                    PisosAVisitar.Text += "⇵ ";
                }
                else
                {
                    if (!piso.Sube_baja)
                    {
                        PisosAVisitar.Text += "↓ ";
                    }
                    else
                    {
                        PisosAVisitar.Text += "↑ ";
                    }
                }
            }
            PisosAVisitar.Text += "}";
            PisosAVisitar.Refresh();
        }

        private void LlegadaAPiso(Peticion actual)
        {
            PisoActualD = actual.Piso;
            if (!actual.Panel)
            {
                Sube_baja = actual.Sube_baja;
            }
            PisoActual.Text = PisoActualD.ToString();
            PisoActual.Refresh();

            //Miramos si hay solicitudes en el piso actual
            var paradasSimilares = paradas.ToList().Where(x => x.Piso == actual.Piso && (x.Sube_baja == actual.Sube_baja || x.Panel));

            /*
             * PT ˄ Sol_atu_pis_act → ObrirPorta
             * 
             * PT ˄ Sol_cri_pis_act → ObrirPorta
             */

            if (PT && (actual != null || paradasSimilares.Count() != 0))
            {

                foreach (var item in paradasSimilares)
                {
                    paradas.Remove(item);
                }

                AbrirPuerta();

                /*
                 * ¬PT ˄ Esp → TancarPorta
                 * 
                 * ¬PT → EsperarXSegons
                 */
                if (!PT)
                {
                    //Esperar un tiempo para que todos puedan entrar
                    System.Threading.Thread.Sleep(2000);

                    CerrarPuerta();
                }

            }
        }

        private void CerrarPuerta()
        {
            AscensorImagen.BackgroundImage = Properties.Resources.AC2;
            AscensorImagen.Refresh();
            PT = true;
        }

        private void AbrirPuerta()
        {
            AscensorImagen.BackgroundImage = Properties.Resources.AA;
            AscensorImagen.Refresh();
            PT = false;
        }

        private void ProgressBar(int piso)
        {
            progressBar.Maximum = Math.Abs(piso - AscensorImagen.Location.Y);
            progressBar.Minimum = 0;
            progressBar.Value = 0;
            progressBar.Step = 1;
        }

        //Mira si hay solicitudes de paradas para el piso (panel del ascensor)
        private bool SolicitudParada(int piso)
        {
            if (paradas.ToList().Where(x => x.Piso == piso && x.Panel == true).Count() != 0) return true;
            return false;
        }

        //Mira si hay solicitudes de llamadas para el piso (desde los botones del piso)
        private bool SolicitudLlamada(int piso)
        {
            if (paradas.ToList().Where(x => x.Piso == piso && x.Panel == false).Count() != 0) return true;
            return false;
        }

        //Mira si hay solicitudes para pisos superiores
        private Peticion SolicitudPisoSuperior(int piso)
        {
            List<Peticion> peticiones = paradas.ToList().Where(x => x.Piso > piso && (x.Sube_baja == Sube_baja || x.Panel)).OrderBy(x => x.Piso).ToList();
            if (peticiones.Count() == 0)
            {
                peticiones = paradas.ToList().Where(x => x.Piso < piso).OrderBy(x => x.Piso).ToList();
            }
            return peticiones.FirstOrDefault();
        }

        //Mira si hay solicitudes para el pisos inferiores
        private Peticion SolicitudPisoInferior(int piso)
        {
            List<Peticion> peticiones = paradas.ToList().Where(x => x.Piso < piso && (x.Sube_baja == Sube_baja || x.Panel)).OrderByDescending(x => x.Piso).ToList();
            if (peticiones.Count() == 0)
            {
                peticiones = paradas.ToList().Where(x => x.Piso < piso).OrderByDescending(x => x.Piso).ToList();
            }
            return peticiones.FirstOrDefault();
        }

        private void Boton1_Click(object sender, EventArgs e)
        {
            //Añadir una parada: el primer valor es el piso, el segundo si es el boton de subir(true) o el de bajar(false) y el tercero indica si el
            //boton es el de dentro del ascensor, en el caso de ser de dentro del ascensor no influye si es de subir o de bajar.
            paradas.Add(new Peticion(1, false, true));
            ActualizarPisosAVisitar();
        }

        private void Boton2_Click(object sender, EventArgs e)
        {
            paradas.Add(new Peticion(2, false, true));
            ActualizarPisosAVisitar();
        }

        private void Boton3_Click(object sender, EventArgs e)
        {
            paradas.Add(new Peticion(3, false, true));
            ActualizarPisosAVisitar();
        }

        private void Boton4_Click(object sender, EventArgs e)
        {
            paradas.Add(new Peticion(4, false, true));
            ActualizarPisosAVisitar();
        }

        private void Boton5_Click(object sender, EventArgs e)
        {
            paradas.Add(new Peticion(5, false, true));
            ActualizarPisosAVisitar();
        }

        private void down5_Click(object sender, EventArgs e)
        {
            paradas.Add(new Peticion(5, false, false));
            ActualizarPisosAVisitar();
        }

        private void up5_Click(object sender, EventArgs e)
        {
            paradas.Add(new Peticion(5, true, false));
            ActualizarPisosAVisitar();
        }

        private void down4_Click(object sender, EventArgs e)
        {
            paradas.Add(new Peticion(4, false, false));
            ActualizarPisosAVisitar();
        }

        private void up4_Click(object sender, EventArgs e)
        {
            paradas.Add(new Peticion(4, true, false));
            ActualizarPisosAVisitar();
        }

        private void down3_Click(object sender, EventArgs e)
        {
            paradas.Add(new Peticion(3, false, false));
            ActualizarPisosAVisitar();
        }

        private void up3_Click(object sender, EventArgs e)
        {
            paradas.Add(new Peticion(3, true, false));
            ActualizarPisosAVisitar();
        }

        private void down2_Click(object sender, EventArgs e)
        {
            paradas.Add(new Peticion(2, false, false));
            ActualizarPisosAVisitar();
        }

        private void up2_Click(object sender, EventArgs e)
        {
            paradas.Add(new Peticion(2, true, false));
            ActualizarPisosAVisitar();
        }

        private void up1_Click(object sender, EventArgs e)
        {
            paradas.Add(new Peticion(1, true, false));
            ActualizarPisosAVisitar();
        }

        private void down6_Click_1(object sender, EventArgs e)
        {
            paradas.Add(new Peticion(6, false, false));
            ActualizarPisosAVisitar();
        }

        private void Boton6_Click_1(object sender, EventArgs e)
        {
            paradas.Add(new Peticion(6, false, true));
            ActualizarPisosAVisitar();
        }

        private void INICIARSIMULACION_Click(object sender, EventArgs e)
        {
            Simulacion = !Simulacion;
            if (Simulacion)
            {
                INICIARSIMULACION.Text = "PARAR SIMULACIÓN";
            }
            else
            {
                INICIARSIMULACION.Text = "INICIAR SIMULACIÓN";
            }
        }
    }
}
