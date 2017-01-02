using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio;
using NAudio.Wave;

namespace Dinofox_Viewer
{
    public partial class Form1 : Form
    {

        String filename;

        int filetype = 0; //order is as follows, starting with 1

        fileTextureDKR texFileDKR;
        fileTextureDB texFileDB;
        fileAudioVox voxFile;
        fileSoundBankDP sbFile;
        //fileSongSequence songFile;
        //fileLevelBlock blockFile;
        //fileModel modelFile;
        //fileObject objectFile;

        WaveOutEvent wavHolder = new WaveOutEvent(); //audio stream holder for NAudio
        Mp3FileReader audReader;
        WaveFileReader sndReader;

        bool switchingSound;

        int treeNum;
        Bitmap curImg;
        Byte[] curVox;

        Byte[] curSnd;


        public Form1()
        {
            InitializeComponent();

            panel1.Hide();
            toolStripStatusLabel1.Text = "Welcome!";
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
        private void loadFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            wavHolder.Stop();
            
            OpenFileDialog binofd = new OpenFileDialog() { /*Filter = "BIN files (*.bin)|*.bin"*/ };
            if (binofd.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

            filename = Path.GetFileNameWithoutExtension(binofd.FileName);

            switch(filename)
            {
                case "TEX":
                    
                    texFileDKR = new fileTextureDKR(binofd.FileName);
                    if (texFileDKR.isValid())
                    {
                        switchPanel(1);
                        toolStripStatusLabel1.Text = "Texture file loaded.";
                    }
                    else
                    {
                        switchPanel(filetype);
                        toolStripStatusLabel1.Text = "Error on loading texture file. TEX.tab not found?";
                    }
                    break;
                case "DB_TEXTURES":
                    texFileDB = new fileTextureDB(binofd.FileName);
                    if(texFileDB.isValid())
                    {
                        switchPanel(4);
                        toolStripStatusLabel1.Text = "Texture file loaded.";
                    }
                    else
                    {
                        switchPanel(filetype);
                        toolStripStatusLabel1.Text = "Error on loading texture file. TEX.tab not found?";
                    }
                    break;

                case "MPEG":
                    voxFile = new fileAudioVox(binofd.FileName);
                    if (voxFile.isValid())
                    {
                        switchPanel(2);
                        toolStripStatusLabel1.Text = "Voice acting file loaded.";
                    }
                    else
                    {
                        switchPanel(filetype);
                        toolStripStatusLabel1.Text = "Error loading audio file. MPEG.tab not found?";
                    }
                    break;

                case "SFX":
                case "AMBIENT":
                case "MUSIC":
                    sbFile = new fileSoundBankDP(binofd.FileName);
                    if(sbFile.isValid())
                    {
                        switchPanel(3);
                        toolStripStatusLabel1.Text = "Soundbank file loaded.";
                    }
                    else
                    {
                        switchPanel(filetype);
                        toolStripStatusLabel1.Text = "Error loading soundbank file. Are the .tab files present?";
                    }
                    break;
                case "AUDIO":
                case "BLOCKS":
                case "MODELS":
                case "OBJECT":

                default:
                    toolStripStatusLabel1.Text = "File " + binofd.FileName + " isn't supported at this time.";
                    break;
            }

            UpdateGUI();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void loadableFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Current loadable files are:" + Environment.NewLine + Environment.NewLine + "TEX.bin (Diddy Kong Racing leftovers)" + Environment.NewLine + "MPEG.bin (Dinosaur Planet voice acting)" + Environment.NewLine + "MUSIC.bin (Dinosaur Planet soundbank) [broken]" + Environment.NewLine + "DB_TEXTURES (leftover Debug textures?)",
                "Loadable Files", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Dinofox Viewer version 0.2.7 by 2Tie" + Environment.NewLine + "Started on 10/25/15" + Environment.NewLine + "Current revision made on 12/2/15" + Environment.NewLine + Environment.NewLine + "Made from scratch recycling some code from xdaniel, pharrox, subdrag, icemario",
                "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void UpdateGUI()
        {
            treeNum = 0;
            treeView1.BeginUpdate();
            treeView1.Nodes.Clear();

            if(filetype == 1)//DKR texture file
            {

                for (int i = 0; i < texFileDKR.addresses.Count-1; i++)
                {
                    TreeNode newnode = new TreeNode(i + ": " + texFileDKR.addresses[i].ToString());
                    newnode.Tag = i;
                    treeView1.Nodes.Add(newnode);
                }

                
            }

            if (filetype == 2)//DP voice acting file
            {

                for (int i = 0; i < voxFile.addresses.Count - 1; i++)
                {
                    TreeNode newnode = new TreeNode(i + ": " + voxFile.addresses[i].ToString());
                    newnode.Tag = i;
                    treeView1.Nodes.Add(newnode);
                }

                
            }

            if (filetype == 3)//DP Soundbank file
            {
                for (int i = 0; i < sbFile.instruments.Count; i++)
                {
                    TreeNode insNode = new TreeNode("Inst. " + i);
                    insNode.Tag = i;
                    for (int j = 0; j < sbFile.instruments[i].sounds.Count; j++)
                    {
                        TreeNode soundNode = new TreeNode("Sound " + j);
                        soundNode.Tag = j;
                        insNode.Nodes.Add(soundNode);
                    }
                    treeView1.Nodes.Add(insNode);
                }
            }
            if (filetype == 4)//DB texture file
            {

                for (int i = 0; i < texFileDB.addresses.Count - 1; i++)
                {
                    TreeNode newnode = new TreeNode(i + ": " + texFileDB.addresses[i].ToString());
                    newnode.Tag = i;
                    treeView1.Nodes.Add(newnode);
                }


            }

            treeView1.EndUpdate();
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if(filetype == 1)//DKR texture file
            {
                treeNum = (int)((TreeView)sender).SelectedNode.Tag;
                curImg = texFileDKR.returnImg(treeNum);
                toolStripStatusLabel1.Text = "Texture " + treeNum + " loaded. W:" + curImg.Width + " H:" + curImg.Height;
                pictureBox1.Image = curImg;
            }
            if (filetype == 2)//DP voice acting file
            {

                switchingSound = true;
                wavHolder.Stop();
                treeNum = (int)((TreeView)sender).SelectedNode.Tag;
                curVox = voxFile.returnClip(treeNum);

                MemoryStream ms = new MemoryStream(curVox);
                audReader = new Mp3FileReader(ms);
                switchingSound = false;
                wavHolder.Init(audReader);
                wavHolder.PlaybackStopped += loopPlayback; 
                wavHolder.Play();

                toolStripStatusLabel1.Text = "Audio clip " + treeNum + " playing.";
            }
            if (filetype == 3)//DP Soundbank File
            {
                switchingSound = true;
                int tmpIns, tmpSnd;
                TreeNode selected = ((TreeView)sender).SelectedNode;

                selected.Expand();
                TreeNode temp = selected.FirstNode; //try to grab a child :3
                if (temp == null) //this is a sound
                {
                    tmpIns = (int)selected.Parent.Tag;
                    tmpSnd = (int)selected.Tag;
                }
                else //this is an instrument
                {
                    tmpIns = (int)selected.Tag;
                    tmpSnd = (int)temp.Tag;
                }

                //decompress audio
                sbFile.dcom(tmpIns, tmpSnd);
                curSnd = sbFile.audioData.ToArray();

                //Console.WriteLine("Sanity Check: " + curSnd[44]);
                //Console.WriteLine(tmpIns + " " + tmpSnd + " Address: {0:X}", sbFile.instruments[tmpIns].sounds[tmpSnd].wavetable.waveBase + sbFile.addresses[1]);

                //load audio
                MemoryStream ms = new MemoryStream(curSnd);
                sndReader = new WaveFileReader(ms);
                switchingSound = false;
                wavHolder.Init(sndReader);

                //play audio
                wavHolder.Play();
            }
            if (filetype == 4)//DB texture file
            {
                treeNum = (int)((TreeView)sender).SelectedNode.Tag;
                curImg = texFileDB.returnImg(treeNum);
                toolStripStatusLabel1.Text = "Texture " + treeNum + " loaded. W:" + curImg.Width + " H:" + curImg.Height;
                pictureBox1.Image = curImg;
            }
        }

        private void switchPanel(int type)
        {

            //wavHolder.Dispose();
            //check filetype, hide whatever panel is relevant to it
            switch(filetype)
            {
                case 1:
                    pictureBox1.Hide();
                    darkBackgroundToolStripMenuItem.Visible = false;
                    if (filetype != type)
                    {
                        texFileDKR = null;
                        pictureBox1.Image = null;
                    }
                    break;

                case 2:
                    panel1.Hide();
                    loopPlaybackToolStripMenuItem.Visible = false;
                    if (filetype != type)
                    {
                        voxFile = null;
                        switchingSound = true;
                    }
                    break;

                case 3:
                    panel1.Hide();
                    loopPlaybackToolStripMenuItem.Visible = false;
                    if (filetype != type)
                    {
                        sbFile = null;
                        switchingSound = true;
                    }
                    break;
                case 4:
                    pictureBox1.Hide();
                    darkBackgroundToolStripMenuItem.Visible = false;
                    if (filetype != type)
                    {
                        texFileDB = null;
                        pictureBox1.Image = null;
                    }
                    break;
            }

            //set filetype to the new (or old) type
            filetype = type;

            //check type, show whatever panel is relevant to it
            switch (filetype)
            {
                case 1:
                    pictureBox1.Show();
                    darkBackgroundToolStripMenuItem.Visible = true;
                    break;

                case 2:
                    panel1.Show();
                    loopPlaybackToolStripMenuItem.Visible = true;
                    break;

                case 3:
                    break;
                default:
                    break;
                case 4:
                    pictureBox1.Show();
                    darkBackgroundToolStripMenuItem.Visible = true;
                    break;
            }
        }

        private void closeFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            wavHolder.Stop();
            switchPanel(0);
            UpdateGUI();
        }

        private void darkBackgroundToolStripMenuItem_Click(object sender, EventArgs e)
        {
            darkBackgroundToolStripMenuItem.Checked = !darkBackgroundToolStripMenuItem.Checked;
            if (darkBackgroundToolStripMenuItem.Checked)
            {
                pictureBox1.BackColor = Color.FromArgb(0,0,0);
            }
            else
                pictureBox1.BackColor = Color.FromName("Control");
        }

        private void loopPlaybackToolStripMenuItem_Click(object sender, EventArgs e)
        {
            loopPlaybackToolStripMenuItem.Checked = !loopPlaybackToolStripMenuItem.Checked;
            
        }
        
        private void loopPlayback(object sender, EventArgs e)
        {
            if ((loopPlaybackToolStripMenuItem.Checked && !switchingSound) && (wavHolder.PlaybackState != PlaybackState.Playing && filetype == 2))
            {
                audReader.Position = 0;
                wavHolder.Play();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (wavHolder.PlaybackState != PlaybackState.Paused)
                audReader.Position = 0;
            wavHolder.Play();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            wavHolder.Pause();
        }
    }
}
