using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace BFRES
{

    public class FTEX : RenderableNode
    {
        GTX.GX2Surface sur;
        public RenderTexture tex = new RenderTexture();

        public FTEX(FileData f)
        {
            ImageKey = "texture";
            SelectedImageKey = "texture";

            if (!f.readString(4).Equals("FTEX"))
                throw new Exception("Error reading Texture");
            sur = new GTX.GX2Surface()
            {
                dim = f.readInt(),
                width = f.readInt(),
                height = f.readInt(),
                depth = f.readInt(),
                numMips = f.readInt(),
                format = f.readInt(),
                aa = f.readInt(),
                use = f.readInt()
            };

            int datalength = f.readInt();
            f.skip(4); // unknown
            sur.mipSize = f.readInt();
            f.skip(4); // unknown
            sur.tileMode = f.readInt();
            sur.swizzle = f.readInt();
            sur.alignment = f.readInt();
            sur.pitch = f.readInt();
            f.skip(0x6C); // unknown
            int dataOff = f.readOffset();
            int mipOff = f.readOffset();
            f.skip(8); //unkown

            sur.data = f.getSection(dataOff, datalength);
            //Console.WriteLine(sur.data.Length + " " + dataOff.ToString("x") + " " + datalength);

            byte[] data = GTX.swizzleBC(sur.data, sur.width, sur.height, sur.format, sur.tileMode, sur.pitch, sur.swizzle);
            tex.mipmaps.Add(data);
            tex.width = sur.width;
            tex.height = sur.height;

            //File.WriteAllBytes(dataOff.ToString("x") + ".bin" ,data);

            switch (sur.format)
            {
                case ((int)GTX.GX2SurfaceFormat.GX2_SURFACE_FORMAT_T_BC1_UNORM):
                    tex.type = PixelInternalFormat.CompressedRgbaS3tcDxt1Ext;
                    break;
                case ((int)GTX.GX2SurfaceFormat.GX2_SURFACE_FORMAT_T_BC1_SRGB):
                    tex.type = PixelInternalFormat.CompressedSrgbAlphaS3tcDxt1Ext;
                    break;
                case ((int)GTX.GX2SurfaceFormat.GX2_SURFACE_FORMAT_T_BC2_UNORM):
                    tex.type = PixelInternalFormat.CompressedRgbaS3tcDxt3Ext;
                    break;
                case ((int)GTX.GX2SurfaceFormat.GX2_SURFACE_FORMAT_T_BC2_SRGB):
                    tex.type = PixelInternalFormat.CompressedSrgbAlphaS3tcDxt3Ext;
                    break;
                case ((int)GTX.GX2SurfaceFormat.GX2_SURFACE_FORMAT_T_BC3_UNORM):
                    tex.type = PixelInternalFormat.CompressedRgbaS3tcDxt5Ext;
                    break;
                case ((int)GTX.GX2SurfaceFormat.GX2_SURFACE_FORMAT_T_BC3_SRGB):
                    tex.type = PixelInternalFormat.CompressedSrgbAlphaS3tcDxt5Ext;
                    break;
                case ((int)GTX.GX2SurfaceFormat.GX2_SURFACE_FORMAT_T_BC4_UNORM):
                    tex.type = PixelInternalFormat.CompressedRedRgtc1;
                    break;
                case ((int)GTX.GX2SurfaceFormat.GX2_SURFACE_FORMAT_T_BC4_SNORM):
                    tex.type = PixelInternalFormat.CompressedSignedRedRgtc1;
                    break;
                case ((int)GTX.GX2SurfaceFormat.GX2_SURFACE_FORMAT_T_BC5_UNORM):
                    tex.type = PixelInternalFormat.CompressedRgRgtc2;
                    break;
                case ((int)GTX.GX2SurfaceFormat.GX2_SURFACE_FORMAT_T_BC5_SNORM):
                    tex.type = PixelInternalFormat.CompressedSignedRgRgtc2;
                    break;
                case ((int)GTX.GX2SurfaceFormat.GX2_SURFACE_FORMAT_TCS_R8_G8_B8_A8_UNORM):
                    tex.type = PixelInternalFormat.Rgba;
                    tex.utype = OpenTK.Graphics.OpenGL.PixelFormat.Rgba;
                    break;
                default:
                    return;
            }

            tex.load();
        }

        public override void Render(Matrix4 v)
        {
            Console.WriteLine("Rendering texture");

            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.BindTexture(TextureTarget.Texture2D, tex.id);

            GL.Begin(PrimitiveType.Quads);
            GL.TexCoord2(1, 1);
            GL.Vertex2(1, -1);
            GL.TexCoord2(0, 1);
            GL.Vertex2(-1, -1);
            GL.TexCoord2(0, 0);
            GL.Vertex2(-1, 1);
            GL.TexCoord2(1, 0);
            GL.Vertex2(1, 1);
            GL.End();
        }
    }


}
