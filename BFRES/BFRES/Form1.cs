using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Timers;

namespace BFRES
{
    public partial class Form1 : Form
    {
        public static int defTex = 0;

        public static ImageList iconList = new ImageList();

        public Form1()
        {
            InitializeComponent();
            System.Timers.Timer timer = new System.Timers.Timer(1000/60);
            timer.Elapsed += OnTimedEvent;
            timer.Start();

            iconList.ImageSize = new Size(24, 24);
            iconList.Images.Add("unk", Properties.Resources.unk);
            iconList.Images.Add("skeleton", Properties.Resources.skeleton);
            iconList.Images.Add("bone", Properties.Resources.bone);
            iconList.Images.Add("model", Properties.Resources.model);
            iconList.Images.Add("polygon", Properties.Resources.polygon);
            iconList.Images.Add("texture", Properties.Resources.texture);
            iconList.Images.Add("material", Properties.Resources.material);
            iconList.Images.Add("animation", Properties.Resources.animation);
            iconList.Images.Add("folder", Properties.Resources.folder);
            iconList.Images.Add("sarc", Properties.Resources.sarc);
            iconList.Images.Add("fres", Properties.Resources.fres);

            treeView1.ImageList = iconList;
            
        }

        public Skeleton loadedSkel = null;
        public AnimationNode loadedAnimation = null;

        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            if(loadedAnimation != null && loadedSkel != null)
            {
                loadedAnimation.nextFrame(loadedSkel);
                glControl1.Invalidate();
            }
        }

        public static int loadImage(Bitmap image)
        {
            int texID = GL.GenTexture();

            GL.BindTexture(TextureTarget.Texture2D, texID);
            BitmapData data = image.LockBits(new System.Drawing.Rectangle(0, 0, image.Width, image.Height),
                ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0,
                OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

            image.UnlockBits(data);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, 2);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            return texID;
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog d = new OpenFileDialog())
            {
                d.Title = "Open File";
                //d.Filter = "SARC|*.sarc";

                if (d.ShowDialog() == DialogResult.OK)
                {
                    treeView1.Nodes.Add(FileBase.ReadFileBase(new FileData(d.FileName)));
                }
            }
        }

        private void treeView1_DoubleClick(object sender, EventArgs e)
        {
            listView1.Visible = false;

            if (treeView1.SelectedNode is FSKANode)
            {
                ((FSKANode)treeView1.SelectedNode).Display(listView1);
            }
            /*if (treeView1.SelectedNode is Skeleton)
            {
                RenderableNodes.Clear();
                RenderableNodes.Add((Skeleton)treeView1.SelectedNode);
                glControl1.Invalidate();
            }
            if (treeView1.SelectedNode is BFRES.FMDL)
            {
                RenderableNodes.Clear();
                RenderableNodes.Add((BFRES.FMDL)treeView1.SelectedNode);
                glControl1.Invalidate();
            }
            if (treeView1.SelectedNode is BFRES.FTEX)
            {
                RenderableNodes.Clear();
                RenderableNodes.Add((BFRES.FTEX)treeView1.SelectedNode);
                glControl1.Invalidate();
            }*/
            if (treeView1.SelectedNode is RenderableNode)
            {
                if(treeView1.SelectedNode is FSKA)
                {
                    List<RenderableNode> mod = new List<RenderableNode>();
                    foreach (RenderableNode node in RenderableNodes)
                        if (node is FMDL)
                            mod.Add(node);
                    RenderableNodes.Clear();
                    RenderableNodes.AddRange(mod);

                    // set transforms

                    loadedSkel = ((FMDL)mod[0]).skel;
                    loadedAnimation = ((FSKA)treeView1.SelectedNode);

                    loadedAnimation.nextFrame(loadedSkel);
                } else
                    RenderableNodes.Clear();
                RenderableNodes.Add((RenderableNode)treeView1.SelectedNode);
                glControl1.Invalidate();
            }else
            if (treeView1.SelectedNode is RenderableNode)
            {
                RenderableNodes.Clear();
                RenderableNodes.Add((RenderableNode)treeView1.SelectedNode);
                glControl1.Invalidate();
            }
            /*if(treeView1.SelectedNode.Tag is FileData)
            {
                using (SaveFileDialog d = new SaveFileDialog())
                {
                    if(d.ShowDialog() == DialogResult.OK)
                    {
                        File.WriteAllBytes(d.FileName, ((FileData)treeView1.SelectedNode.Tag).b);
                    }
                }
            }*/
        }

        // rendering
        List<RenderableNode> RenderableNodes = new List<RenderableNode>();
        float rot = 0;
        float lookup = 0;
        float height = 1;
        float width = 0;
        float zoom = -20f;
        float mouseXLast = 0;
        float mouseYLast = 0;
        float mouseSLast = 0;
        Matrix4 v;

        private void glControl1_Paint(object sender, PaintEventArgs e)
        {
            glControl1.MakeCurrent();

            GL.ClearColor(Color.DarkSlateGray);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();

            GL.Enable(EnableCap.DepthTest);
            GL.ClearDepth(1.0);

            v = Matrix4.CreateRotationY(0.5f * rot) * Matrix4.CreateRotationX(0.2f * lookup) * Matrix4.CreateTranslation(5 * width, -5f - 5f * height, zoom) * Matrix4.CreatePerspectiveFieldOfView(1.3f, glControl1.Width / (float)glControl1.Height, 1.0f, 1000.0f);

            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref v);
            GL.MatrixMode(MatrixMode.Modelview);

            GL.Disable(EnableCap.Texture2D);
            DrawFloor();
            GL.Enable(EnableCap.Texture2D);

            foreach (RenderableNode node in RenderableNodes)
            {
                node.Render(v);
            }

            glControl1.SwapBuffers();
        }

        public static void DrawFloor()
        {
            GL.LineWidth(1f);
            GL.Color3(Color.LightGray);
            GL.Begin(PrimitiveType.Lines);
            for (var i = -10; i <= 10; i++)
            {
                GL.Vertex3(new Vector3(-10f * 2, 0f, i * 2));
                GL.Vertex3(new Vector3(10f * 2, 0f, i * 2));
                GL.Vertex3(new Vector3(i * 2, 0f, -10f * 2));
                GL.Vertex3(new Vector3(i * 2, 0f, 10f * 2));
            }
            GL.End();
            GL.Color3(Color.Transparent);
        }

        private void Form1_Resize(object sender, EventArgs e)
        {

            GL.LoadIdentity();
            GL.Viewport(glControl1.ClientRectangle);
        }

        private void glControl1_Scroll(object sender, ScrollEventArgs e)
        {
            Console.WriteLine(e.NewValue);
            zoom += (e.NewValue - e.OldValue);
            glControl1.Invalidate();
        }

        private void glControl1_Load(object sender, EventArgs e)
        {
            defTex = loadImage(new Bitmap("C:\\Users\\ploaj_000\\Desktop\\proguard5.2.1\\CharacterThingy\\default.png"));
        }

        private void treeView1_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            if(e.KeyChar == 's')
            {
                if(treeView1.SelectedNode is FileBase)
                {
                    using (SaveFileDialog d = new SaveFileDialog())
                    {
                        d.Title = "Save File";
                        //d.Filter = "SARC|*.sarc";

                        if (d.ShowDialog() == DialogResult.OK)
                        {
                            if(treeView1.SelectedNode is BFRES)
                            {
                                ((BFRES)treeView1.SelectedNode).Rebuild(d.FileName);
                            }
                            else
                                ((FileBase)treeView1.SelectedNode).Save(d.FileName);
                        }
                    }
                }
            }
        }

        private void treeView1_Click(object sender, EventArgs e)
        {
            glControl1.Invalidate();
        }

        private void glControl1_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            if (e.KeyChar == 's')
                zoom -= 5;
            if (e.KeyChar == 'w')
                zoom += 5;
            glControl1.Invalidate();
        }
    }

    public abstract class RenderableNode : TreeNode
    {
        abstract public void Render(Matrix4 v);
    }
}
