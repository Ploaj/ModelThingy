using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace BFRES
{
    public class FMDL : RenderableNode
    {
        List<FMAT> mats = new List<FMAT>();
        List<FVTX> vertattr = new List<FVTX>();
        List<FSHP> shapes = new List<FSHP>();
        public FSKL skel;

        public FMDL(FileData f)
        {
            ImageKey = "model";
            SelectedImageKey = "model";

            f.skip(4); // MAGIC
            int fnameOffset = f.readOffset();
            Text = f.readString(fnameOffset, -1);
            f.skip(4); // pointer to end of string table?
            int FSKLOffset = f.readOffset();
            int FVTXOffset = f.readOffset();
            int FSHPOffset = f.readOffset();
            int FMATOffset = f.readOffset();
            int PARAMOffset = f.readOffset();
            int FVTXCount = f.readShort();
            int FSHPCount = f.readShort();
            int FMATCount = f.readShort();
            int PARAMCount = f.readShort();
            f.skip(4); // unknown possible facecount or something?

            // firstly Imma do the skeleton
            f.seek(FSKLOffset);
            skel = new FSKL();
            skel.Read(f);
            Nodes.Add(skel);

            // FMAT is obs materials
            f.seek(FMATOffset);
            IndexGroup fmatGroup = new IndexGroup(f);
            for (int i = 0; i < FMATCount; i++)
            {
                f.seek(fmatGroup.dataOffsets[i]);
                mats.Add(new FMAT(f));
            }

            // FVTX is the vertex buffer object attributes
            f.seek(FVTXOffset);
            for (int i = 0; i < FVTXCount; i++)
            {
                vertattr.Add(new FVTX(f));
            }

            // FSHP is the mesh objects
            f.seek(FSHPOffset);
            IndexGroup fmdlGroup = new IndexGroup(f);
            for (int i = 0; i < FSHPCount; i++)
            {
                f.seek(fmdlGroup.dataOffsets[i]);
                shapes.Add(new FSHP(f));
            }

            Nodes.AddRange(shapes.ToArray());
            Nodes.AddRange(vertattr.ToArray());
            Nodes.AddRange(mats.ToArray());

            GL.GenBuffers(1, out ibo);
        }

        int ibo;

        public override void Render(Matrix4 v)
        {
            //Console.WriteLine("Rendering " + Text);
            GL.UseProgram(BFRES.shader.programID);

            GL.UniformMatrix4(BFRES.shader.getAttribute("modelview"), false, ref v);

            FSKL skel = ((FMDL)shapes[0].Parent).skel;

            Matrix4[] f = skel.getBoneTransforms();
            int[] bind = skel.bindId;
            GL.UniformMatrix4(BFRES.shader.getAttribute("bones"), f.Length, false, ref f[0].Row0.X);
            GL.Uniform1(BFRES.shader.getAttribute("bonematch"), bind.Length, ref bind[0]);

            BFRES.shader.enableAttrib();
            foreach (FSHP shape in shapes)
            {
                FVTX vert = vertattr[shape.fvtxindex];

                string tex = mats[shape.fmatIndex].tex[0].Text;
                // find it in textures
                foreach(TreeNode n in ((BFRES)Parent.Parent).Nodes)
                {
                    if (n.Text.Equals("FTEXs"))
                    {
                        foreach (TreeNode no in n.Nodes)
                        {
                            if(no.Text.Equals(tex))
                            {
                                Console.WriteLine("Binding " + no.Text);
                                GL.ActiveTexture(TextureUnit.Texture0);
                                GL.BindTexture(TextureTarget.Texture2D, ((FTEX)no).tex.id);
                                GL.Uniform1(BFRES.shader.getAttribute("tex"), 0);
                                break;
                            }
                        }
                        break;
                    }
                }
                

                GL.BindBuffer(BufferTarget.ArrayBuffer, vert.gl_vbo);
                GL.VertexAttribPointer(BFRES.shader.getAttribute("_p0"), 3, VertexAttribPointerType.Float, false, Vertex.Stride, 0);
                GL.VertexAttribPointer(BFRES.shader.getAttribute("_n0"), 3, VertexAttribPointerType.Float, false, Vertex.Stride, 12);
                GL.VertexAttribPointer(BFRES.shader.getAttribute("_u0"), 2, VertexAttribPointerType.Float, false, Vertex.Stride, 24);
                GL.VertexAttribPointer(BFRES.shader.getAttribute("_i0"), 4, VertexAttribPointerType.Float, false, Vertex.Stride, 32);
                GL.VertexAttribPointer(BFRES.shader.getAttribute("_w0"), 4, VertexAttribPointerType.Float, false, Vertex.Stride, 48);
                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

                // bind attributes
                //Console.WriteLine(shape.Text + " " + shape.singleBind);
                GL.Uniform1(BFRES.shader.getAttribute("single"), shape.singleBind);
                /*foreach (BFRESAttribute att in vert.attributes)
                {
                    int size = 0;
                    BFRESBuffer buffer = vert.buffers[att.bufferIndex];
                    float[] data = att.data.ToArray();
                    //Console.WriteLine(att.Text + " " + ((int)(att.format)).ToString("x"));
                    switch (att.Text)
                    {
                        case "_p0": GL.BindBuffer(BufferTarget.ArrayBuffer, vbo_po); size = 4; break;
                        case "_n0": GL.BindBuffer(BufferTarget.ArrayBuffer, vbo_n0); size = 4; break;
                        case "_i0":
                            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo_i0);
                            if (att.format == 256) { size = 1; } else
                            if (att.format == 260) { size = 2; } else
                            //if (att.format == 261) { size = 4; } else
                            if (att.format == 266) { size = 4; } else
                            if (att.format == 268) { size = 2; }
                            //else if (att.format == 272) { size = 4; }
                            else { Console.WriteLine("Unused bone type "); }
                            GL.Uniform1(BFRES.shader.getAttribute("boneSize"), size);

                            for (int i = 0; i < att.data.Count; i++)
                            {
                                if (data[i] < skel.bindId.Count)
                                    data[i] = skel.bindId[(int)data[i]];
                            }
                            break;
                        case "_w0":
                            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo_w0);
                            if (att.format == 4) { size = 2; }
                            if (att.format == 10) { size = 4; }
                            if (att.format == 2061) { size = 2; }
                            break;
                        default: continue;
                    }
                    //Console.WriteLine(buffer.stride);
                    GL.BufferData<float>(BufferTarget.ArrayBuffer, (IntPtr)(data.Length * sizeof(float)), data, BufferUsageHint.StaticDraw);
                    GL.VertexAttribPointer(BFRES.shader.getAttribute(att.Text), size, VertexAttribPointerType.Float, false, 
                        buffer.stride * BFRESAttribute.formatStrideMultiplyer[att.format], att.bufferOffset);// 
                }*/

                // draw models
                foreach (LODModel mod in shape.lodModels)
                {
                    GL.BindBuffer(BufferTarget.ElementArrayBuffer, ibo);

                    if (mod.type == DrawElementsType.UnsignedShort)
                    {
                        GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(mod.data.Length * sizeof(ushort)), mod.data, BufferUsageHint.StaticDraw);
                        GL.DrawElements(PrimitiveType.Triangles, mod.fcount, mod.type, mod.skip * sizeof(ushort));
                    }
                    else
                    if (mod.type == DrawElementsType.UnsignedInt)
                    {
                        GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(mod.data.Length * sizeof(uint)), mod.data, BufferUsageHint.StaticDraw);
                        GL.DrawElements(PrimitiveType.Triangles, mod.fcount, mod.type, mod.skip * sizeof(uint));
                    }

                    break; // only draw first one
                }
            }
            BFRES.shader.disableAttrib();

            GL.UseProgram(0);
            GL.Disable(EnableCap.DepthTest);
            skel.Render(v);
            GL.Enable(EnableCap.DepthTest);
        }
    }

    public class FSKL : Skeleton
    {
        public int[] bindId;

        public FSKL()
        {
            ImageKey = "skeleton";
            SelectedImageKey = "skeleton";
        }

        public void Read(FileData f)
        {
            Text = "Skeleton";
            if (!f.readString(4).Equals("FSKL"))
                throw new Exception("Error reading Skeleton");

            f.skip(4); // 0 and either 0x1100 or 0x1200 version?
            int bcount = f.readShort();
            int inverseIndexCount = f.readShort();
            int extraCount = f.readShort();
            f.skip(2); // unused always 0 padding
            int boneIndexGroupOffset = f.readOffset();
            int boneArrayOffset = f.readOffset();
            int inverseIndexOffset = f.readOffset();
            int inverseMatrixOffset = f.readOffset();
            f.skip(4); // padding 0

            f.seek(inverseIndexOffset);
            bindId = new int[inverseIndexCount];
            for (int i = 0; i < inverseIndexCount; i++)
                bindId[i] = (f.readShort());

            // now read bones
            f.seek(boneArrayOffset);
            Bone[] bones = new Bone[bcount];
            for (int i = 0; i < bcount; i++)
            {
                Bone b = new Bone();
                b.Text = f.readString(f.readOffset(), -1);
                bones[i] = b;
                b.id = f.readShort();
                b.p1 = (short)f.readShort();
                b.p2 = (short)f.readShort();
                b.p3 = (short)f.readShort();
                b.p4 = (short)f.readShort();
                f.skip(2); // padding 0
                f.skip(2); // TODO: flags
                f.skip(2); // dunno, 0x1001
                b.sca = new Vector3(f.readFloat(),
                    f.readFloat(),
                    f.readFloat());
                b.rot = new Vector3(f.readFloat(),
                    f.readFloat(),
                    f.readFloat());
                f.skip(4); // for quat, with eul is 1.0
                b.pos = new Vector3(f.readFloat(),
                    f.readFloat(),
                    f.readFloat());
                f.skip(4); // padding

                //TODO:
                if (BFRES.low < 4)
                    f.skip(48);
            }
            Nodes.AddRange(bones);
            Reset();
            //PrintDepth();
        }
    }

    #region Vertex Attributes

    public struct Vertex
    {
        public float x, y, z;
        public float nx, ny, nz;
        public Vector2 uv0;
        public float i1, i2, i3, i4; // can have 5
        public float w1, w2, w3, w4;
        public const int Stride = 4 * 16;
    }

    public class FVTX : TreeNode
    {
        public List<BFRESBuffer> buffers = new List<BFRESBuffer>();
        public List<BFRESAttribute> attributes = new List<BFRESAttribute>();
        public int vertCount;
        public int unk1;

        // okay, so because converting between buffers can get complicated, Imma use my own system
        public List<Vertex> data = new List<Vertex>();
        public int gl_vbo;

        public FVTX(FileData f)
        {
            Text = "VertexAttributeBuffer";
            if (!f.readString(4).Equals("FVTX"))
                throw new Exception("Error reading Skeleton");
            int attrCount = f.readByte();
            int bufferCount = f.readByte();
            int index = f.readShort();
            vertCount = f.readInt();
            //f.skip(4); // dunno
            //Console.WriteLine("Byte " + f.readByte() + " " + attrCount + " " + bufferCount);
            unk1 = f.readByte();
            f.skip(3);
            int attrArrayOffset = f.readOffset();
            int attrIndexGroupOffset = f.readOffset();
            int bufferOffset = f.readOffset();
            f.skip(4); // padding

            int temp = f.pos();

            f.seek(bufferOffset);
            for (int i = 0; i < bufferCount; i++)
                buffers.Add(new BFRESBuffer(f));

            f.seek(attrArrayOffset);
            for (int i = 0; i < attrCount; i++)
            {
                attributes.Add(new BFRESAttribute(f, buffers));
            }

            Nodes.AddRange(attributes.ToArray());

            myRender();

            f.seek(temp);
        }

        private class TempVertex
        {
            public float x = 0, y = 0, z = 0;
            public float nx = 0, ny = 0, nz = 0;
            public Vector2 uv0 = new Vector2();
            public float i1 = 0, i2 = 0, i3 = 0, i4 = 0; // can have 5
            public float w1 = 0, w2 = 0, w3 = 0, w4 = 0;
        }

        private void myRender()
        {
            GL.GenBuffers(1, out gl_vbo);

            for (int i = 0; i < vertCount; i++)
            {
                TempVertex vert = new TempVertex();

                foreach (BFRESAttribute att in attributes)
                {
                    FileData d = new FileData(new FileData(buffers[att.bufferIndex].data).getSection(0, -1));
                    d.seek(att.bufferOffset + i * buffers[att.bufferIndex].stride);
                    switch (att.format)
                    {
                        /*
                        case 0x10C: data.Add(d.readFloat()); break;
                        case 0x20A: data.Add(((sbyte)d.readByte()) / 128); break;
                        case 0x80D: data.Add(d.readFloat()); break;
                        case 0x813: data.Add(d.readFloat()); break;*/
                        case 0x004:
                            vert.w1 = d.readByte() / (float)255;
                            vert.w2 = d.readByte() / (float)255;
                            break;
                        case 0x007:
                            if (att.Text.Equals("_u2"))
                            {
                                vert.uv0.X = d.readShort() / (float)0xFFFF;
                                vert.uv0.Y = d.readShort() / (float)0xFFFF;
                            } else
                            {
                                vert.w1 = d.readShort() / (float)0xFFFF;
                                vert.w2 = d.readShort() / (float)0xFFFF;
                            }
                            break;
                        case 0x00A:
                            vert.w1 = d.readByte() / (float)255;
                            vert.w2 = d.readByte() / (float)255;
                            vert.w3 = d.readByte() / (float)255;
                            vert.w4 = d.readByte() / (float)255;
                            break;
                        case 0x100:
                            vert.i1 = d.readByte();
                            vert.w1 = 1;
                            break;
                        case 0x104:
                            vert.i1 = d.readByte();
                            vert.i2 = d.readByte();
                            break;
                        case 0x10A:
                            vert.i1 = d.readByte();
                            vert.i2 = d.readByte();
                            vert.i3 = d.readByte();
                            vert.i4 = d.readByte();
                            break;
                        case 0x20B:
                            int normVal = (int)d.readInt();
                            vert.nx = FileData.sign10Bit((normVal) & 0x3FF) / 511f;
                            vert.ny = FileData.sign10Bit((normVal >> 10) & 0x3FF) / 511f;
                            vert.nz = FileData.sign10Bit((normVal >> 20) & 0x3FF) / 511f;
                            break;
                        case 0x80F:
                            vert.x = d.readHalfFloat();
                            vert.y = d.readHalfFloat();
                            vert.z = d.readHalfFloat();
                            d.readHalfFloat(); //w
                            break;
                        case 0x811:
                            vert.x = d.readFloat();
                            vert.y = d.readFloat();
                            vert.z = d.readFloat();
                            d.readFloat(); //w
                            break;
                        default:
                            //d.skip(d.size());
                            //Console.WriteLine(Text + " Unknown type " + att.format.ToString("x") + " 0x" + (att.bufferOffset + buffers[bufferIndex].dataOffset).ToString("x"));
                            break;
                    }
                }
                data.Add(new Vertex()
                {
                    x = vert.x, y = vert.y, z = vert.z,
                    nx = vert.nx, ny = vert.ny, nz = vert.nz,
                    uv0 = vert.uv0,
                    i1 = vert.i1, i2 = vert.i2, i3 = vert.i3, i4 = vert.i4,
                    w1 = vert.i1, w2 = vert.i2, w3 = vert.i3, w4 = vert.i4,
                });
            }

            GL.BindBuffer(BufferTarget.ArrayBuffer, gl_vbo);
            GL.BufferData<Vertex>(BufferTarget.ArrayBuffer, (IntPtr)(data.Count * Vertex.Stride), data.ToArray(), BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }
    }

    public class BFRESAttribute : TreeNode
    {
        public int bufferIndex, bufferOffset;
        public int format;

        public static Dictionary<int, int> formatStrideMultiplyer = new Dictionary<int, int>()
        {
            {4, 4 },
            {7, 2 },
            {10, 4 },
            {0x100, 4 },
            {0x104, 4 },
            {0x10A, 4 },
            {0x10C, 1 },
            {2061, 1 },
            {2063, 2 },
            {2065, 1 },
            {2067, 1 },
            {0x20A, 4 },
            {0x20B, 4 },
        };

        public BFRESAttribute(FileData f, List<BFRESBuffer> buffers)
        {
            Text = f.readString(f.readOffset(), -1);
            bufferIndex = f.readByte();
            bufferOffset = f.readThree();
            format = f.readInt();

            //Text += " 0x" + format.ToString("x");

            //Console.WriteLine(Text + " " + bufferOffset.ToString("x") + " " + format + " " + buffers[bufferIndex].dataOffset.ToString("x") + " " + buffers[bufferIndex].stride);
            Console.WriteLine(Text + " type " + format.ToString("x"));
            /*FileData d = new FileData(new FileData(buffers[bufferIndex].data).getSection(bufferOffset, -1));
            while (d.pos() < d.size())
            {
                switch (format)
                {
                    case 0x004:
                        data.Add(d.readByte() / 255f);
                        break;
                    case 0x007: data.Add(d.readShort() / 0xFFFF); break;
                    case 0x00A: data.Add(d.readByte() / 255f); break;
                    case 0x100: data.Add(d.readByte()); break;
                    case 0x104: data.Add(d.readByte()); break;
                    case 0x10A: data.Add(d.readByte()); break;
                    case 0x10C: data.Add(d.readFloat()); break;
                    case 0x20A: data.Add(((sbyte)d.readByte()) / 128); break;
                    case 0x20B:
                        uint normVal = (uint)d.readInt();
                        data.Add(((normVal & 0x3FC00000) >> 22) / 511f);
                        data.Add(((normVal & 0x000FF000) >> 12) / 511f);
                        data.Add(((normVal & 0x000003FC) >> 2) / 511f);
                        data.Add(1);
                        break;
                    case 0x80D: data.Add(d.readFloat()); break;
                    case 0x80F: data.Add(d.readHalfFloat()); break;
                    case 0x811: data.Add(d.readFloat()); break;
                    case 0x813: data.Add(d.readFloat()); break;
                    default:
                        d.skip(d.size());
                        Console.WriteLine(Text + " Unknown type " + format.ToString("x") + " 0x" + (bufferOffset + buffers[bufferIndex].dataOffset).ToString("x"));
                        break;
                }
            }*/
        }
    }

    public class BFRESBuffer : TreeNode
    {
        public int size, stride;
        public int dataOffset;
        public byte[] data;

        public int gl_vbo;

        public BFRESBuffer(FileData f)
        {
            f.skip(4); // padding 0
            size = f.readInt();
            f.skip(4); // padding 0
            stride = f.readShort();
            f.skip(2); // always 1
            f.skip(4); // padding 0
            dataOffset = f.readOffset();
            data = f.getSection(dataOffset, size);
        }
    }

    #endregion

    public class FSHP : TreeNode
    {
        public int fvtxindex;
        public int fvtxOffset;
        public int fmatIndex;
        public List<LODModel> lodModels = new List<LODModel>();
        public int singleBind;

        public FSHP(FileData f)
        {
            ImageKey = "polygon";
            SelectedImageKey = "polygon";

            if (!f.readString(4).Equals("FSHP"))
                throw new Exception("Error reading Mesh Shape");
            Text = f.readString(f.readOffset(), -1);

            f.skip(4); // 2
            int index = f.readShort();
            fmatIndex = f.readShort();
            singleBind = (short)f.readShort();
            fvtxindex = f.readShort();
            int fsklarraycount = f.readShort();
            f.skip(1); // depends on the above
            int lodCount = f.readByte();
            int visGroupCount = f.readInt();
            float unk1 = f.readFloat();
            fvtxOffset = f.readOffset();
            int lodOffset = f.readOffset();
            int fsklIndexArrayOffset = f.readOffset();
            f.skip(4); // padding 0
            f.skip(12); // visgroups nodes-ranges-indices
            f.skip(4); // padding 0

            // level of detail models
            f.seek(lodOffset);
            //IndexGroup fmdlGroup = new IndexGroup(f);
            for (int i = 0; i < lodCount; i++)
            {
                lodModels.Add(new LODModel(f));
            }
            Nodes.AddRange(lodModels.ToArray());

            // visibility group

        }

    }

    public class LODModel : TreeNode
    {
        public int faceType, indexBufferOffset, skip;
        public List<int> faces = new List<int>();
        public DrawElementsType type = DrawElementsType.UnsignedShort;
        public ushort[] data;
        public uint[] dataui;
        public int fcount;

        public LODModel(FileData f)
        {
            Text = "DetailLevel";
            f.skip(4); // 4 
            faceType = f.readInt();
            int count = f.readInt();
            int visgroup = f.readShort();
            f.skip(2); // padding
            int visoffset = f.readOffset();
            int faceOffset = f.readOffset(); // is indexes
            skip = f.readInt();

            int temp = f.pos();

            f.seek(faceOffset);
            f.skip(4); // 0 padding
            fcount = f.readInt() / 2;
            f.skip(4); // 0 padding
            f.skip(4); // 0 then 1 padding
            f.skip(4); // 0 padding
            indexBufferOffset = f.readOffset();
            f.seek(indexBufferOffset);
            data = new ushort[fcount];
            dataui = new uint[fcount];
            for (int i = 0; i < fcount; i++)
            {
                if (faceType == 4)
                    data[i] = (ushort)f.readShort();
                else
                if (faceType == 9)
                {
                    dataui[i] = (uint)f.readInt();
                    type = DrawElementsType.UnsignedInt;
                }
                else
                    throw new Exception("Unknown face types " + faceType);
            }

            f.seek(temp);
        }
    }


    public class FMAT : TreeNode
    {
        public int sectionindex;
        public List<TextureSelecter> tex = new List<TextureSelecter>();

        public FMAT(FileData f)
        {
            ImageKey = "material";
            SelectedImageKey = "material";

            if (!f.readString(4).Equals("FMAT"))
                throw new Exception("Error reading Material");
            Text = f.readString(f.readOffset(), -1);

            f.skip(4); // always 1?
            sectionindex = f.readShort();
            int paramCount = f.readShort();
            int texselectCount = f.readByte();
            int texattCount = f.readByte();
            int matParamCount = f.readShort();
            int matParamSize = f.readInt();
            f.skip(4); // unk 0, 1, or 2
            int renderparamindexgroup = f.readOffset();
            int unkMatStructOff = f.readOffset();
            int shaderControlOffset = f.readOffset();
            int texselectOffset = f.readOffset();
            int texattOffset = f.readOffset();
            int texattIndexGroupOffset = f.readOffset();
            int matParamOffset = f.readOffset();
            int matParamIndexGroupOffset = f.readOffset();
            int matParamDataOffset = f.readOffset();
            int shadowParamIndexGroup = f.readOffset();
            f.skip(4); //unknown offset to 12 bytes all 0?

            f.seek(texselectOffset);
            for(int i = 0; i < texselectCount; i++)
            {
                tex.Add(new TextureSelecter(f));
            }
        }
    }

    public class TextureSelecter : TreeNode
    {

        public TextureSelecter(FileData f)
        {
            Text = f.readString(f.readOffset(), -1);
            int ftexoffset = f.readOffset();
        }

    }

}
