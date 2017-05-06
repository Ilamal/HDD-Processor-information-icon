using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Management;
using System.Management.Instrumentation;
using System.Collections.Specialized;
using System.Threading;
using System.Diagnostics;

namespace Tietojenilmoitusohjelma
{
    public partial class InvisibleForm : Form
    {
        #region Global variables
        NotifyIcon nIcon;
        Icon HDDBusyIcon;
        Icon HDDidleIcon;
        Thread HDDInfoThread;
        Thread ProcessorInfoThread;
        static List<Thread> threads;
        static bool hddRun, cpuRun;
        static String startThread;
        static String polku = "c:\\work\\settings.txt";
        #endregion

        #region Forms
        public InvisibleForm()
        {
            InitializeComponent();
            threads = new List<Thread>();

            // Ladataan iconit tiedostoista 
            HDDBusyIcon = new Icon("HDD_Busy.ico");
            HDDidleIcon = new Icon("HDD_Idle.ico");

            // Luo notifyiconin näkyvänä ja asettaa sen idleksi
            nIcon = new NotifyIcon();
            nIcon.Icon = HDDidleIcon;
            nIcon.Visible = true;

            // Lukee tiedostosta aloitusThreadin
            try
            {
                System.IO.StreamReader fileR = new System.IO.StreamReader(polku);
                startThread = fileR.ReadLine();
                fileR.Close();
            }
            catch(System.IO.FileNotFoundException ex)
            {
                startThread = "hddStart";
            }

            //
            // Luo menut, sen itemit ja lisää ne iconin valikkoon
            //
            MenuItem creatorMenuItem = new MenuItem("Infoilmoitin by Ilari Malinen v. 0.1 ALPHA");
            MenuItem quitMenuItem = new MenuItem("Quit");
            MenuItem processorSearch = new MenuItem("Ilmoita prosessorin käyttö");
            MenuItem hddInfo = new MenuItem("Ilmoita HDD - driven käyttö");
            MenuItem settings = new MenuItem("Settings");
            ContextMenu contextM = new ContextMenu();          
            contextM.MenuItems.Add(creatorMenuItem);
            contextM.MenuItems.Add(hddInfo);
            contextM.MenuItems.Add(processorSearch);
            contextM.MenuItems.Add(settings);
            contextM.MenuItems.Add(quitMenuItem);          
            nIcon.ContextMenu = contextM;

            // Toiminnallisuus painikkeille
            quitMenuItem.Click += QuitMenuItem_Click;
            processorSearch.Click += ProcessorSearch_Click;
            hddInfo.Click += HddInfo_Click;
            settings.Click += Settings_Click;

            //
            // Piilotetaan ikkuna, sitä ei alkuun haluta
            //
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;

            //Luo threadit
            //aloittaa halutun ensimmäisen threadin
            HDDInfoThread = new Thread(new ThreadStart(HddActivityThread));
            threads.Add(HDDInfoThread);
            ProcessorInfoThread = new Thread(new ThreadStart(ProcessorActivityThread));
            threads.Add(ProcessorInfoThread);
            if(startThread == "hddStart")
            {
                hddRun = true;
                HDDInfoThread.Start();
            }
            else if (startThread == "cpuStart")
            {
                cpuRun = true;
                ProcessorInfoThread.Start();
            }
            
            label2.Text = "Nykyinen: " + startThread;
        }

       


        #endregion

        #region EventHandlers (Ikoni menu napit)

        /// <summary>
        /// Sulkee ohjelman kun painetaan 'quit' näppäintä ikoni valikossa
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void QuitMenuItem_Click(object sender, EventArgs e)
        {
            endThreads();            // sammuttaa threadit
            int vika = threads.Count - 1;
            if (threads[vika].ThreadState == System.Threading.ThreadState.Running)
                threads[vika].Join();  // odottaa että viimeinen thread on sammunut
            nIcon.Dispose();         // poistaa iconin kummittelemasta
            this.Close();            // sulkee itse ohjelman
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProcessorSearch_Click(object sender, EventArgs e)
        {
            endThreads();
            //        if(ProcessorInfoThread.ThreadState == System.Threading.ThreadState.Running)  
            //            ProcessorInfoThread.Join();
            
            try
            {
                ProcessorInfoThread = new Thread(new ThreadStart(ProcessorActivityThread));
                cpuRun = true;
                ProcessorInfoThread.Start();
            }
            catch (ThreadStartException)
            {
                MessageBox.Show("RÖJÄHTI");
            }
            catch (ThreadStateException)
            {

            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HddInfo_Click(object sender, EventArgs e)
        {
            endThreads();
            //        if (HDDInfoThread.ThreadState == System.Threading.ThreadState.Running)
            //           HDDInfoThread.Join();
            
            try
            {
                HDDInfoThread = new Thread(new ThreadStart(HddActivityThread));
                hddRun = true;
                HDDInfoThread.Start();
            }
            catch (ThreadStartException)
            {
                MessageBox.Show("RÖJÄHTI");
            }
            catch (ThreadStateException)
            {

            }
        }

        private void Settings_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
        }
        /// <summary>
        /// hdd alkuoletus
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            System.IO.StreamWriter fileW = new System.IO.StreamWriter("c:\\work\\settings.txt");
            fileW.WriteLine("hddStart");
            fileW.Close();
            label2.Text = "Nykyinen: " + startThread; //nykyinen tekstin asetus
            MessageBox.Show("HDD:n ilmoittaminen asetettu oletukseksi! (tiedosto sijainnissta: " + polku + ")");
        }
        //prosessori oletus
        private void button2_Click(object sender, EventArgs e)
        {
            System.IO.StreamWriter fileW = new System.IO.StreamWriter("c:\\work\\settings.txt");
            fileW.WriteLine("cpuStart");
            fileW.Close();
            label2.Text = "Nykyinen: " + startThread; // nykyinen tekstin asetus
            MessageBox.Show("CPU:n ilmoittaminen asetettu oletukseksi! (tiedosto sijainnissta: " + polku + ")");
        }
        /// <summary>
        /// Close button (sammuttaa ikkunan)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button4_Click(object sender, EventArgs e)
        {
            //ikkuna näkymättömäksi
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
        }
        /// <summary>
        /// About nappi (tietoja)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Infoilmoitin by Ilari Malinen v. 0.1 ALPHA\nKlikkaamalla valikosta haluamasi ilmoitettava"+
                "info tallentuu ja se jatkaa oletuksena.\nHalutessasi vaihtaa info vain tälle istunnolle onnistuu se ikonivalikosta\n"+
                "Prosessorin käytön ylittyessä 80% näkyy kuvake punaisena ja HDD:n ollessa käytössä näkyy kuvake punaisena");
        }

        #endregion

        #region threads
        /// <summary>
        ///  Threadi kiintolevyn käytön tutkimiseen
        /// </summary>
        public void HddActivityThread()                 
        {
            ManagementClass driveDataClass = new ManagementClass("Win32_PerfFormattedData_PerfDisk_PhysicalDisk");
            try
            {
                while (hddRun)
                {
                    //Hae kovalevy performanssi ote
                    ManagementObjectCollection driveDataClassCollection = driveDataClass.GetInstances();
                    foreach (ManagementObject obj in driveDataClassCollection)
                    {
                        // tutki ainoastaan _Total objektin DiskBytesPersec arvoa
                        if (obj["Name"].ToString() == "_Total")
                        {
                            if (Convert.ToUInt64(obj["DiskBytesPersec"]) > 0)
                            {
                                nIcon.Icon = HDDBusyIcon;
                            }
                            else
                            {
                                try { 
                                nIcon.Icon = HDDidleIcon;
                                } catch (NullReferenceException ex)
                                  {

                                  }
                            }

                        }
                    }

                    Thread.Sleep(150);  // Performance säätö
                }

            } catch (ThreadAbortException ex)
            {
                MessageBox.Show("SULJETTU");
                driveDataClass.Dispose();   //varmistaa ettei jää kellumaan sammuttaessa
            }
            catch (ThreadInterruptedException ex)
            {
                driveDataClass.Dispose();  //varmistaa siivouksen sammuttaessa
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void ProcessorActivityThread()
        {
            // hakee prossorin kulutuksen prosentteina
            PerformanceCounter cpuCounter = new PerformanceCounter("Processor Information", "% Processor Time", "_Total");
            try
            {
                    while (cpuRun)
                    {
                        if (cpuCounter.NextValue() > 80)
                        {
                            //muuta ikoni
                            nIcon.Icon = HDDBusyIcon;
                           
                    }
                        else
                        {
                            // muuta ikoni
                            nIcon.Icon = HDDidleIcon;
                    }
                        Thread.Sleep(150); // Performance säätö
                    }
                
            }
            catch (ThreadAbortException ex)
            {
                MessageBox.Show("SULJETTU");
                cpuCounter.Dispose(); //varmistaa siivouksen sammuttaessa
            }
            catch (ThreadInterruptedException ex)
            {
                MessageBox.Show("INTERRUPTED");
                cpuCounter.Dispose(); //varmistaa siivouksen sammuttaessa
            }

         }


        #endregion

        #region static functions
        static public void pauseThreads()
        {
            for(int i = 0; i < threads.Count; i++)
            {
                threads[i].Interrupt();
            }
        }


        //sammuttaa threadit
        static public void endThreads()
        {
            cpuRun = false;
            hddRun = false;
        }
        #endregion

    }
}

