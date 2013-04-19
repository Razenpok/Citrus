using System;
using System.IO;
using System.Collections.Generic;
#if iOS
using OpenTK.Graphics.ES11;
#elif MAC
using MonoMac.OpenGL;
using OpenTK.Graphics.OpenGL;
#elif WIN
using OpenTK.Graphics.OpenGL;
#endif

namespace Lime
{
#if MAC
	using OGL = GL;
#endif
	/// <summary>
	/// Represents 2D texture
	/// </summary>
	public partial class Texture2D : ITexture
	{
		uint id;
		public Size ImageSize { get; protected set; }
		public Size SurfaceSize { get; protected set; }
		Rectangle uvRect;

		public static List<uint> TexturesToDelete = new List<uint>();

		public static void DeleteScheduledTextures()
		{
			lock (TexturesToDelete) {
				if (TexturesToDelete.Count > 0) {
					var ids = new uint[TexturesToDelete.Count];
					TexturesToDelete.CopyTo(ids);
#if GLES11
					GL.DeleteTextures(ids.Length, ids);
#else
					OGL.DeleteTextures(ids.Length, ids);
#endif
					TexturesToDelete.Clear();
					Renderer.CheckErrors();
				}
			}
		}

		public string SerializationPath {
			get {
				throw new NotSupportedException();
			}
			set {
				throw new NotSupportedException();
			}
		}

		public Rectangle UVRect { get { return uvRect; } }

		public bool IsStubTexture { get { return false; } }

		public void LoadImage(string path)
		{
			using (Stream stream = PackedAssetsBundle.Instance.OpenFileLocalized(path)) {
				LoadImage(stream);
			}
		}

		public void LoadImage(Stream stream)
		{
			// Discard current texture
			Dispose();
			DeleteScheduledTextures();

#if GLES11
			// Generate a new texture.
			GL.GenTextures(1, ref id);
			Renderer.SetTexture(id, 0);
			GL.TexParameter(All.Texture2D, All.TextureMinFilter, (int)All.Linear);
			GL.TexParameter(All.Texture2D, All.TextureMagFilter, (int)All.Linear);
			GL.TexParameter(All.Texture2D, All.TextureWrapS, (int)All.ClampToEdge);
			GL.TexParameter(All.Texture2D, All.TextureWrapT, (int)All.ClampToEdge);
			GL.Hint(All.PerspectiveCorrectionHint, All.Fastest);
#else
			// Generate a new texture
			id = (uint)OGL.GenTexture();
			Renderer.SetTexture(id, 0);
			OGL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
			OGL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
			OGL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureParameterName.ClampToEdge);
			OGL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureParameterName.ClampToEdge);
			OGL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Fastest);
#endif
			using (RewindableStream rewindableStream = new RewindableStream(stream))
			using (BinaryReader reader = new BinaryReader(rewindableStream)) {
#if iOS
				int sign = reader.ReadInt32();
				rewindableStream.Rewind();
				if (sign == PVRMagic) {
					InitWithPVRTexture(reader);
				} else {
					InitWithPngOrJpgBitmap(rewindableStream);
				}
#else
				int sign = reader.ReadInt32();
				rewindableStream.Rewind();
				if (sign == DDSMagic) {
					InitWithDDSBitmap(reader);
				} else {
					InitWithPngOrJpgBitmap(rewindableStream);
				}
#endif
			}
			Renderer.CheckErrors();
			uvRect = new Rectangle(Vector2.Zero, (Vector2)ImageSize / (Vector2)SurfaceSize);
		}

		/// <summary>
		/// Create texture from pixel array
		/// </summary>
		public void LoadImage(Color4[] pixels, int width, int height, bool generateMips)
		{
			// Discards current texture.
			if (width != ImageSize.Width || height != ImageSize.Height) {
				Dispose();
				DeleteScheduledTextures();
				// Generate a new texture id
#if GLES11
				GL.GenTextures(1, ref id);
#else
				id = (uint)OGL.GenTexture();
#endif
			}
#if GLES11
			Renderer.SetTexture(id, 0);
			GL.TexParameter(All.Texture2D, All.TextureMinFilter, (int)All.Linear);
			GL.TexParameter(All.Texture2D, All.TextureMagFilter, (int)All.Linear);
			GL.TexParameter(All.Texture2D, All.TextureWrapS, (int)All.ClampToEdge);
			GL.TexParameter(All.Texture2D, All.TextureWrapT, (int)All.ClampToEdge);
			GL.Hint(All.PerspectiveCorrectionHint, All.Fastest);
			GL.TexImage2D(All.Texture2D, 0, (int)All.Rgba, width, height, 0,
				All.Rgba, All.UnsignedByte, pixels);
#else
			Renderer.SetTexture(id, 0);
			OGL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
			OGL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
			OGL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureParameterName.ClampToEdge);
			OGL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureParameterName.ClampToEdge);
			OGL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Fastest);
			OGL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0,
				PixelFormat.Rgba, PixelType.UnsignedByte, pixels);
			if (generateMips) {
#if WIN
				OGL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
#else
				Console.WriteLine("WARNING: Mipmap generation is not implemented for this platform");
#endif
			}
#endif
			Renderer.CheckErrors();

			ImageSize = new Size(width, height);
			SurfaceSize = ImageSize;
			uvRect = new Rectangle(0, 0, 1, 1);
		}

		~Texture2D()
		{
			Dispose();
		}

		public void Dispose()
		{
			if (id != 0) {
				lock (TexturesToDelete) {
					TexturesToDelete.Add(id);
				}
				id = 0;
			}
		}

		/// <summary>
		/// Returns native texture handle
		/// </summary>
		/// <returns></returns>
		public uint GetHandle()
		{
			return id;
		}

		/// <summary>
		/// Sets texture as a render target
		/// </summary>
		public void SetAsRenderTarget()
		{
		}

		/// <summary>
		/// Restores default render target(backbuffer).
		/// </summary>
		public void RestoreRenderTarget()
		{
		}

		/// <summary>
		/// Checks pixel transparency at given coordinates
		/// </summary>
		/// <param name="x">x-coordinate of pixel</param>
		/// <param name="y">y-coordinate of pixel</param>
		/// <returns></returns>
		public bool IsTransparentPixel(int x, int y)
		{
			return false;
		}
	}
}