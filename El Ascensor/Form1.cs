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
        private int PisoActual = 1; //Dato del piso actual
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

        private bool PT = true; //Indica si la puerta esta cerrada (true) o abierta (false)
        public ElAscensor()
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            LblPisoActual.Text = PisoActual.ToString(); //Imprimimos por pantalla el piso actual

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

                        //Selecciona el piso en base a la base de conocimientos
                        Peticion peticion = SeleccionarPiso();

                        if (EnMarcha != true)
                        {
                            //Indicacion de cuanto falta para llegar al piso
                            BarraProgreso(peticion.Piso);
                            EnMarcha = true;
                            ActualizarPanelSubeBaja();
                        }

                        //Si se ha pulsado el piso actual no hacer nada
                        if (PisoActual < peticion.Piso)
                        {
                            SubirUnPiso();
                        }
                        else if (PisoActual > peticion.Piso)
                        {
                            BajarUnPiso();
                        }
                        else
                        {
                            //Llega al piso y actualiza los pisos pendientes y se abre y cierra la puerta
                            ActualizarPanelSubeBaja();
                            LlegadaAPiso(peticion);
                            ActualizarPanelPisosAVisitar();
                        }
                        progressBar.PerformStep();
                    }
                    EnMarcha = false;
                    ActualizarPanelSubeBaja();
                    BarraProgreso(0);
                }
            }
        }

        private void SubirUnPiso()
        {
            //Indicamos que sube y actualizamos las luces de subir o bajar
            if (Sube_baja != true)
            {
                Sube_baja = true;
                ActualizarPanelSubeBaja();
            }
            while (AscensorImagen.Location.Y > (int)paradasY[PisoActual + 1]) {
                AscensorImagen.Location = new Point(AscensorImagen.Location.X, AscensorImagen.Location.Y - 1);
                System.Threading.Thread.Sleep(10);
            }
            PisoActual = PisoActual + 1;
        }

        private void BajarUnPiso()
        {
            if (Sube_baja != false)
            {
                //Indicamos que baja y actualizamos las luces de subir o bajar
                Sube_baja = false;
                ActualizarPanelSubeBaja();
            }
            while (AscensorImagen.Location.Y < (int)paradasY[PisoActual - 1])
            {
                {
                    AscensorImagen.Location = new Point(AscensorImagen.Location.X, AscensorImagen.Location.Y + 1);
                    System.Threading.Thread.Sleep(10);
                }
            }
            PisoActual = PisoActual - 1;
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
             * Si existe alguna solicitud de parada y/o llamada en el piso superior
             */

            /*
             * 5. PujBai ˄ PT ˄ ¬Sol_atu_pis_act ˄ Sol_atu_pis_sup → PujarUnPis
             * 6. PujBai ˄ PT ˄ ¬Sol_cri_pis_act ˄ Sol_cri_pis_sup →  PujarUnPis
             */

            if (Sube_baja && PT && !SolicitudParada(PisoActual) && !SolicitudLlamada(PisoActual))
            {
                //objetivo = SolicitudPisoInferior(PisoActual);
                objetivo = SolicitudPisoSuperior(PisoActual);
                if (objetivo != null)
                {
                    LblPisoObjetivo.Text = objetivo.Piso.ToString();
                    LblPisoObjetivo.Refresh();
                    return objetivo;
                }
            }


            //Bajar un piso-------------------------------------------------------
            /*
                * El ascensor siempre espera a cerrar la puerta para tomar una decisión
                * Miramos si no hay solicitudes de parada ni de llamada para el piso actual
                * Si existe alguna solicitud de parada y/o llamada en el piso inferior
                *
                * 7. ¬PujBai ˄ PT ˄ ¬Sol_atu_pis_act ˄ Sol_atu_pis_inf → BaixarUnPis
                * 8. ¬PujBai ˄ PT ˄ ¬Sol_cri_pis_act ˄ Sol_cri_pis_inf → BaixarUnPis
                * 9. PujBai ˄ PT ˄ ¬Sol_atu_pis_act ˄ Sol_atu_pis_inf → BaixarUnPis 
                * 10. PujBai ˄ PT ˄ ¬Sol_cri_pis_act ˄ Sol_cri_pis_inf → BaixarUnPis 
             */

            if (PT && !SolicitudParada(PisoActual) && !SolicitudLlamada(PisoActual))
            {
                objetivo = SolicitudPisoInferior(PisoActual);
                if (objetivo != null)
                {
                    LblPisoObjetivo.Text = objetivo.Piso.ToString();
                    LblPisoObjetivo.Refresh();
                    return objetivo;
                }
            }

            //Subir un piso------------------------------------------------------
            /*
             * 11. PujBai ˄ PT ˄ ¬Sol_atu_pis_act ˄ Sol_atu_pis_sup → PujarUnPis
             * 12. PujBai ˄ PT ˄ ¬Sol_cri_pis_act ˄ Sol_cri_pis_sup →  PujarUnPis
            */
            if (Sube_baja && PT && !SolicitudParada(PisoActual) && !SolicitudLlamada(PisoActual)){

                objetivo = SolicitudPisoSuperior(PisoActual);

                if (objetivo != null)
                {
                    LblPisoObjetivo.Text = objetivo.Piso.ToString();
                    LblPisoObjetivo.Refresh();
                    return objetivo;
                }
            }

            // Llamada desde el piso actual
            if (SolicitudLlamada(PisoActual) || SolicitudParada(PisoActual))
            {
                objetivo = paradas.ToList().Where(x => x.Piso == PisoActual).FirstOrDefault();
            }


            if (objetivo == null)
            {
                objetivo = paradas[0]; //escoge el primer piso si no hay ninguno seleccionado o error
            }

            LblPisoObjetivo.Text = objetivo.Piso.ToString();
            LblPisoObjetivo.Refresh();
            return objetivo;
        }

        private void ActualizarPanelSubeBaja()
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
        private void ActualizarPanelPisosAVisitar()
        {
            LblPisosAVisitar.Text = "{ ";
            foreach (Peticion piso in paradas)
            {
                LblPisosAVisitar.Text += piso.Piso.ToString();
                if (piso.Panel)
                {
                    LblPisosAVisitar.Text += "⇵ ";
                }
                else
                {
                    if (!piso.Sube_baja)
                    {
                        LblPisosAVisitar.Text += "↓ ";
                    }
                    else
                    {
                        LblPisosAVisitar.Text += "↑ ";
                    }
                }
            }
            LblPisosAVisitar.Text += "}";
            LblPisosAVisitar.Refresh();
        }

        private void LlegadaAPiso(Peticion actual)
        {
            PisoActual = actual.Piso;
            if (!actual.Panel)
            {
                Sube_baja = actual.Sube_baja;
            }
            LblPisoActual.Text = PisoActual.ToString();
            LblPisoActual.Refresh();

            //Miramos si hay solicitudes en el piso actual
            var paradasSimilares = paradas.ToList().Where(x => x.Piso == actual.Piso && (x.Sube_baja == actual.Sube_baja || x.Panel));

            /*
                * 1. PT ˄ Sol_atu_pis_act → ObrirPorta
                * 
                * 2. PT ˄ Sol_cri_pis_act → ObrirPorta
                */

            if (PT && (actual != null || paradasSimilares.Count() != 0))
            {

                foreach (var item in paradasSimilares)
                {
                    paradas.Remove(item);
                }

                AbrirPuerta();

                /*
                * 3. ¬PT ˄ Esp → TancarPorta
                * 
                * 4. ¬PT → EsperarXSegons
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

        private void BarraProgreso(int piso)
        {
            progressBar.Maximum = Math.Abs(PisoActual - piso);
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
            ActualizarPanelPisosAVisitar();
        }

        private void Boton2_Click(object sender, EventArgs e)
        {
            paradas.Add(new Peticion(2, false, true));
            ActualizarPanelPisosAVisitar();
        }

        private void Boton3_Click(object sender, EventArgs e)
        {
            paradas.Add(new Peticion(3, false, true));
            ActualizarPanelPisosAVisitar();
        }

        private void Boton4_Click(object sender, EventArgs e)
        {
            paradas.Add(new Peticion(4, false, true));
            ActualizarPanelPisosAVisitar();
        }

        private void Boton5_Click(object sender, EventArgs e)
        {
            paradas.Add(new Peticion(5, false, true));
            ActualizarPanelPisosAVisitar();
        }

        private void Boton6_Click(object sender, EventArgs e)
        {
            paradas.Add(new Peticion(6, false, true));
            ActualizarPanelPisosAVisitar();
        }

        private void down5_Click(object sender, EventArgs e)
        {
            paradas.Add(new Peticion(5, false, false));
            ActualizarPanelPisosAVisitar();
        }

        private void up5_Click(object sender, EventArgs e)
        {
            paradas.Add(new Peticion(5, true, false));
            ActualizarPanelPisosAVisitar();
        }

        private void down4_Click(object sender, EventArgs e)
        {
            paradas.Add(new Peticion(4, false, false));
            ActualizarPanelPisosAVisitar();
        }

        private void up4_Click(object sender, EventArgs e)
        {
            paradas.Add(new Peticion(4, true, false));
            ActualizarPanelPisosAVisitar();
        }

        private void down3_Click(object sender, EventArgs e)
        {
            paradas.Add(new Peticion(3, false, false));
            ActualizarPanelPisosAVisitar();
        }

        private void up3_Click(object sender, EventArgs e)
        {
            paradas.Add(new Peticion(3, true, false));
            ActualizarPanelPisosAVisitar();
        }

        private void down2_Click(object sender, EventArgs e)
        {
            paradas.Add(new Peticion(2, false, false));
            ActualizarPanelPisosAVisitar();
        }

        private void up2_Click(object sender, EventArgs e)
        {
            paradas.Add(new Peticion(2, true, false));
            ActualizarPanelPisosAVisitar();
        }

        private void up1_Click(object sender, EventArgs e)
        {
            paradas.Add(new Peticion(1, true, false));
            ActualizarPanelPisosAVisitar();
        }

        private void down6_Click_1(object sender, EventArgs e)
        {
            paradas.Add(new Peticion(6, false, false));
            ActualizarPanelPisosAVisitar();
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
