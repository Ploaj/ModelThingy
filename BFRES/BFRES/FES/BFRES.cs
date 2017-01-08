using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace BFRES
{
    class BFRES : FileBase
    {
        public string ModelName = "";

        public static string vs = @"
#version 330

in vec3 _p0;
in vec3 _n0;
in vec2 _u0;
in vec4 _i0;
in vec4 _w0;

out vec4 n;
out vec2 u0;

uniform mat4 modelview;
uniform mat4[100] bones;
uniform int[100] bonematch;
uniform int single;

vec4 skin()
{
    ivec4 b = ivec4(_i0);
    vec4 pos = vec4(_p0, 1.0);

    vec4 trans = bones[bonematch[b.x]] * pos * _w0.x;
    trans += bones[bonematch[b.y]] * pos *_w0.y;
    trans += bones[bonematch[b.z]] * pos *_w0.z;
    if(_w0.w < 1)
        trans += bones[bonematch[b.w]] * pos *_w0.w;
        
    return trans;
}

void main()
{
    n = vec4(_n0.xyz, 1);
    u0 = _u0;

    gl_Position = modelview * skin();
}";

        public static string fs = @"
#version 330

in vec4 n;
in vec2 u0;

uniform sampler2D tex;

void fresnel()
{
    
}

void main()
{
gl_FragData[0] = texture2D(tex, u0);
}";


        public static Shader shader = null;

        public BFRES(FileData f)
        {
            ImageKey = "fres";
            SelectedImageKey = "fres";
            if(shader == null)
            {
                shader = new Shader();
                shader.vertexShader(vs);
                shader.fragmentShader(fs);
                shader.addAttribute("_p0", false);
                shader.addAttribute("_n0", false);
                shader.addAttribute("_u0", false);
                shader.addAttribute("_w0", false);
                shader.addAttribute("_i0", false);
                shader.addAttribute("modelview", true);
                shader.addAttribute("bones", true);
                shader.addAttribute("bonematch", true);
                shader.addAttribute("single", true);
                shader.addAttribute("tex", true);
            }
            Text = f.fname;
            Tag = f;
            Read(f);
        }

        public static int low, high, overall;

        // note: offsets are self relative
        public void Read(FileData f)
        {
            data = f;
            f.Endian = Endianness.Big;
            f.seek(4); // magic check
            high = f.readByte();
            low = f.readByte();
            overall = f.readShort(); // overall version
            if (f.readShort() == 0xFEFF)
                f.Endian = Endianness.Big;
            else f.Endian = Endianness.Little;
            f.skip(2); // version number? 0x0010
            f.skip(4); // file length
            f.skip(4); // file alignment usuallt 0x00002000
            /*Note, alignment is for gpu addresses so not important for this*/

            Text = f.readString(f.readOffset(), -1);
            // 
            Console.WriteLine("Reading " + ModelName);

            f.skip(4); // string table length
            int stringTableOffset = f.readOffset();

            int FMDLOffset = f.readOffset();
            int FTEXOffset = f.readOffset();
            int FSKAOffset = f.readOffset();
            int FSHU0Offset = f.readOffset();
            int FSHU1Offset = f.readOffset();
            int FSHU2Offset = f.readOffset();
            int FTXPOffset = f.readOffset();
            int FVIS0Offset = f.readOffset();
            int FVIS1Offset = f.readOffset();
            int FSHAOffset = f.readOffset();
            int FSCNOffset = f.readOffset();
            int EMBOffset = f.readOffset();

            int FMDLCount = f.readShort();
            int FTEXCount = f.readShort();
            int FSKACount = f.readShort();
            int FSHU0Count = f.readShort();
            int FSHU1Count = f.readShort();
            int FSHU2Count = f.readShort();
            int FTXPCount = f.readShort();
            int FVIS0Count = f.readShort();
            int FVIS1Count = f.readShort();
            int FSHACount = f.readShort();
            int FSCNCount = f.readShort();
            int EMBCount = f.readShort();

            // INDEX GROUPS
            f.seek(FMDLOffset);
            IndexGroup fmdlGroup = new IndexGroup(f);
            TreeNode modelGroup = new TreeNode();
            modelGroup.Text = "FMDLs";
            modelGroup.ImageKey = "folder";
            modelGroup.SelectedImageKey = "folder";
            Nodes.Add(modelGroup);
            for (int i = 0; i < FMDLCount; i++)
            {
                f.seek(fmdlGroup.dataOffsets[i]);
                modelGroup.Nodes.Add(new FMDL(f));
            }
            
            f.seek(FTEXOffset);
            IndexGroup ftexGroup = new IndexGroup(f);
            TreeNode texGroup = new TreeNode();
            texGroup.Text = "FTEXs";
            texGroup.ImageKey = "folder";
            texGroup.SelectedImageKey = "folder";
            Nodes.Add(texGroup);
            for (int i = 0; i < FTEXCount; i++)
            {
                f.seek(ftexGroup.dataOffsets[i]);
                texGroup.Nodes.Add(new FTEX(f) { Text = ftexGroup.names[i] });
            }

            f.seek(FSKAOffset);
            IndexGroup fskaGroup = new IndexGroup(f);
            TreeNode animGroup = new TreeNode();
            animGroup.Text = "FSKAs";
            animGroup.ImageKey = "folder";
            animGroup.SelectedImageKey = "folder";
            Nodes.Add(animGroup);
            for (int i = 0; i < FSKACount; i++)
            {
                f.seek(fskaGroup.dataOffsets[i]);
                Console.WriteLine(fskaGroup.names[i] + " 0x" + fskaGroup.dataOffsets[i].ToString("x"));
                animGroup.Nodes.Add(new FSKA(f));
            }

            f.seek(FSHU0Offset);
            IndexGroup fshu0Group = new IndexGroup(f);
            TreeNode shu0Group = new TreeNode();
            shu0Group.Text = "FSHU0s";
            shu0Group.ImageKey = "folder";
            shu0Group.SelectedImageKey = "folder";
            Nodes.Add(shu0Group);
            for (int i = 0; i < FSHU0Count; i++)
            {
                //f.seek(fshu0Group.dataOffsets[i]);
                shu0Group.Nodes.Add(new TreeNode() { Text = fshu0Group.names[i] });
            }

            f.seek(FSHU1Offset);
            //IndexGroup fshu1Group = new IndexGroup(f);
            TreeNode shu1Group = new TreeNode();
            shu1Group.Text = "FSHU1s";
            shu1Group.ImageKey = "folder";
            shu1Group.SelectedImageKey = "folder";
            Nodes.Add(shu1Group);
            for (int i = 0; i < FSHU1Count; i++)
            {
                //f.seek(fshu1Group.dataOffsets[i]);
                //shu1Group.Nodes.Add(new TreeNode() { Text = fshu1Group.names[i] });
            }

            f.seek(FSHU2Offset);
            IndexGroup fshu2Group = new IndexGroup(f);
            TreeNode shu2Group = new TreeNode();
            shu2Group.Text = "FSHU2s";
            shu2Group.ImageKey = "folder";
            shu2Group.SelectedImageKey = "folder";
            Nodes.Add(shu2Group);
            for (int i = 0; i < FSHU2Count; i++)
            {
                //f.seek(fshu2Group.dataOffsets[i]);
                shu2Group.Nodes.Add(new TreeNode() { Text = fshu2Group.names[i] });
            }
            
            f.seek(FTXPOffset);
            IndexGroup ftxpGroup = new IndexGroup(f);
            TreeNode txpGroup = new TreeNode();
            txpGroup.Text = "FTXPs";
            txpGroup.ImageKey = "folder";
            txpGroup.SelectedImageKey = "folder";
            Nodes.Add(txpGroup);
            for (int i = 0; i < FTXPCount; i++)
            {
                //f.seek(ftxpGroup.dataOffsets[i]);
                txpGroup.Nodes.Add(new TreeNode() { Text = ftxpGroup.names[i] });
            }

            f.seek(FVIS0Offset);
            IndexGroup fvis0Group = new IndexGroup(f);
            TreeNode vis0Group = new TreeNode();
            vis0Group.Text = "FVIS0s";
            vis0Group.ImageKey = "folder";
            vis0Group.SelectedImageKey = "folder";
            Nodes.Add(vis0Group);
            for (int i = 0; i < FVIS0Count; i++)
            {
                //f.seek(fvis0Group.dataOffsets[i]);
                vis0Group.Nodes.Add(new TreeNode() { Text = fvis0Group.names[i] });
            }
            
            f.seek(FVIS1Offset);
            IndexGroup fvis1Group = new IndexGroup(f);
            TreeNode vis1Group = new TreeNode();
            vis1Group.Text = "FVIS1s";
            vis1Group.ImageKey = "folder";
            vis1Group.SelectedImageKey = "folder";
            Nodes.Add(vis1Group);
            for (int i = 0; i < FVIS1Count; i++)
            {
                //f.seek(fvis1Group.dataOffsets[i]);
                vis1Group.Nodes.Add(new TreeNode() { Text = fvis1Group.names[i] });
            }

            f.seek(FSHAOffset);
            //IndexGroup fshaGroup = new IndexGroup(f);
            TreeNode shaGroup = new TreeNode();
            shaGroup.Text = "FSHAs";
            shaGroup.ImageKey = "folder";
            shaGroup.SelectedImageKey = "folder";
            Nodes.Add(shaGroup);
            for (int i = 0; i < FSHACount; i++)
            {
                //f.seek(fshaGroup.dataOffsets[i]);
                //shaGroup.Nodes.Add(new TreeNode() { Text = fshaGroup.names[i] });
            }

            f.seek(FSCNOffset);
            IndexGroup fscnGroup = new IndexGroup(f);
            TreeNode scnGroup = new TreeNode();
            scnGroup.Text = "FSCNs";
            scnGroup.ImageKey = "folder";
            scnGroup.SelectedImageKey = "folder";
            Nodes.Add(scnGroup);
            for (int i = 0; i < FSCNCount; i++)
            {
                //f.seek(fscnGroup.dataOffsets[i]);
                scnGroup.Nodes.Add(new TreeNode() { Text = fscnGroup.names[i] });
            }

            /*f.seek(EMBOffset);
            IndexGroup fembGroup = new IndexGroup(f);
            TreeNode embGroup = new TreeNode();
            embGroup.Text = "Embedded Files";
            embGroup.ImageKey = "folder";
            embGroup.SelectedImageKey = "folder";
            Nodes.Add(embGroup);
            for (int i = 0; i < EMBCount; i++)
            {
                //f.seek(fembGroup.dataOffsets[i]);
                embGroup.Nodes.Add(new TreeNode() { Text = fembGroup.names[i] });
            }*/
        }

        public void Rebuild(string fname)
        {
            FileOutput o = new FileOutput();
            FileOutput h = new FileOutput();
            FileOutput d = new FileOutput();
            FileOutput s = new FileOutput();

            // bfres header
            o.writeString("FRES");
            o.writeByte(high);
            o.writeByte(low);
            o.writeShort(overall);
            o.writeShort(0xFEFF); // endianness
            o.writeShort(0x0010);// version number? 0x0010
            int fileSize = o.size();
            o.writeInt(0);// file length
            o.writeInt(0x00002000);// file alignment usuallt 0x00002000
            o.writeOffset(s.getStringOffset(Text), s);

            int stringTableSize = o.size();
            o.writeInt(0);
            int stringTableOffset = o.size();
            o.writeInt(0);
            
            o.writeInt(0);o.writeInt(0);o.writeInt(0);o.writeInt(0);
            o.writeInt(0);o.writeInt(0);o.writeInt(0);o.writeInt(0);
            o.writeInt(0);o.writeInt(0);o.writeInt(0); o.writeInt(0);

            o.writeShort(0); o.writeShort(0); o.writeShort(0); o.writeShort(0);
            o.writeShort(0); o.writeShort(0); o.writeShort(0); o.writeShort(0);
            o.writeShort(0); o.writeShort(0); o.writeShort(0); o.writeShort(0);

            foreach (TreeNode n in Nodes)
            {
                if(n.Text.Equals("FMDLs"))
                {
                    o.writeIntAt(o.size(), 0x20);
                    o.writeShortAt(n.Nodes.Count, 0x50);

                    IndexGroup group = new IndexGroup();
                    // create an index group and save it  
                    foreach(FMDL mdl in n.Nodes)
                    {
                        group.nodes.Add(mdl);
                    }
                    group.Save(o, h, s, d);
                }
            }

            o.writeOutput(h);
            o.writeIntAt(o.size(), stringTableOffset);
            o.writeIntAt(s.size(), stringTableSize);
            o.writeOutput(s);
            o.writeOutput(d);
            o.writeIntAt(o.size(), fileSize);
            o.save(fname);
        }
        
    }
}
