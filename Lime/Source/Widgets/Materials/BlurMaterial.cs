namespace Lime
{
	public class BlurMaterial : IMaterial
	{
		private readonly Blending blending;
		private readonly ShaderParams[] shaderParamsArray;
		private readonly ShaderParams shaderParams;
		private readonly ShaderParamKey<float> blurringXKey;
		private readonly ShaderParamKey<float> blurringYKey;
		private readonly ShaderParamKey<Vector2> dirKey;
		private readonly ShaderParamKey<float> alphaCorrectionKey;

		public float Radius { get; set; } = 1f;
		public Vector2 Step { get; set; } = Vector2.One * (1f / 128f);
		public Vector2 Dir { get; set; } = new Vector2(0, 1);
		public float AlphaCorrection { get; set; } = 1f;

		public int PassCount => 1;

		public BlurMaterial() : this(Blending.Alpha) { }

		public BlurMaterial(Blending blending)
		{
			this.blending = blending;
			shaderParams = new ShaderParams();
			shaderParamsArray = new[] { Renderer.GlobalShaderParams, shaderParams };
			blurringXKey = shaderParams.GetParamKey<float>("blurringX");
			blurringYKey = shaderParams.GetParamKey<float>("blurringY");
			dirKey = shaderParams.GetParamKey<Vector2>("dir");
			alphaCorrectionKey = shaderParams.GetParamKey<float>("inversedAlphaCorrection");
		}

		public void Apply(int pass)
		{
			shaderParams.Set(blurringXKey, Radius * Step.X);
			shaderParams.Set(blurringYKey, Radius * Step.Y);
			shaderParams.Set(dirKey, Dir);
			shaderParams.Set(alphaCorrectionKey, 1f / AlphaCorrection);
			PlatformRenderer.SetBlendState(blending.GetBlendState());
			PlatformRenderer.SetShaderProgram(BlurShaderProgram.GetInstance());
			PlatformRenderer.SetShaderParams(shaderParamsArray);
		}

		public void Invalidate() { }

		public IMaterial Clone()
		{
			return new BlurMaterial(blending) {
				Radius = Radius,
				Step = Step,
				Dir = Dir
			};
		}
	}

	public class BlurShaderProgram : ShaderProgram
	{
		private const string VertexShader = @"
			attribute vec4 inPos;
			attribute vec4 inColor;
			attribute vec2 inTexCoords1;

			uniform mat4 matProjection;

			varying lowp vec4 color;
			varying lowp vec2 texCoords1;

			void main()
			{
				gl_Position = matProjection * inPos;
				color = inColor;
				texCoords1 = inTexCoords1;
			}
			";

		private const string FragmentShader = @"
			varying lowp vec4 color;
			varying lowp vec2 texCoords1;

			uniform lowp sampler2D tex1;
			uniform lowp float blurringX;
			uniform lowp float blurringY;
			uniform lowp vec2 dir;
			uniform lowp float inversedAlphaCorrection;

			void main() {
				lowp vec4 sum = vec4(0.0);
				lowp vec2 tc = texCoords1;
				lowp float hstep = dir.x;
				lowp float vstep = dir.y;

				sum += texture2D(tex1, vec2(tc.x - 4.0*blurringX*hstep, tc.y - 4.0*blurringY*vstep)) * 0.0162162162;
				sum += texture2D(tex1, vec2(tc.x - 3.0*blurringX*hstep, tc.y - 3.0*blurringY*vstep)) * 0.0540540541;
				sum += texture2D(tex1, vec2(tc.x - 2.0*blurringX*hstep, tc.y - 2.0*blurringY*vstep)) * 0.1216216216;
				sum += texture2D(tex1, vec2(tc.x - 1.0*blurringX*hstep, tc.y - 1.0*blurringY*vstep)) * 0.1945945946;

				sum += texture2D(tex1, vec2(tc.x, tc.y)) * 0.2270270270;

				sum += texture2D(tex1, vec2(tc.x + 1.0*blurringX*hstep, tc.y + 1.0*blurringY*vstep)) * 0.1945945946;
				sum += texture2D(tex1, vec2(tc.x + 2.0*blurringX*hstep, tc.y + 2.0*blurringY*vstep)) * 0.1216216216;
				sum += texture2D(tex1, vec2(tc.x + 3.0*blurringX*hstep, tc.y + 3.0*blurringY*vstep)) * 0.0540540541;
				sum += texture2D(tex1, vec2(tc.x + 4.0*blurringX*hstep, tc.y + 4.0*blurringY*vstep)) * 0.0162162162;

				sum.w = pow(sum.w, inversedAlphaCorrection);
				gl_FragColor = color * sum;
			}
			";

		private static BlurShaderProgram instance;

		public static BlurShaderProgram GetInstance() => instance ?? (instance = new BlurShaderProgram());

		private BlurShaderProgram() : base(CreateShaders(), ShaderPrograms.Attributes.GetLocations(), ShaderPrograms.GetSamplers()) { }

		private static Shader[] CreateShaders() => new Shader[] {
			new VertexShader(VertexShader),
			new FragmentShader(FragmentShader)
		};
	}
}
