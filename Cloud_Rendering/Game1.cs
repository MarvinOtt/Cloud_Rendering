using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SharpDX.Direct3D11;
using BlendState = Microsoft.Xna.Framework.Graphics.BlendState;
using Texture3D = Microsoft.Xna.Framework.Graphics.Texture3D;

namespace Cloud_Rendering
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Game1 : Game
    {
        public static GraphicsDeviceManager graphics;
        public static ContentManager content;
        public static Game1 ME;
        public static System.Windows.Forms.Form form;
        SpriteBatch spriteBatch;

        private Effect cloud_effect;
        private VertexBuffer cloudbox;
        private RenderTarget2D maintarget;
        private Matrix boxworld;
        private Texture3D tex;
        Perlin perlin = new Perlin(0, 3);
        private float currenttime = 0;
        private Vector3 LightDir;
        private float SunRot;



        public static int Screenwidth = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;
        public static int Screenheight = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;

        #region IO

        KeyboardState oldkeyboardstate, keyboardstate;
        private Vector2 cameramousepos, mouserotationbuffer;
        private System.Drawing.Point mousepointpos;

        #endregion

        #region Camera

        private static Vector3 camerapos = new Vector3(0, 0, 0), camerarichtung;
        private Matrix camview, camworld, camprojection;
        private BasicEffect cameraeffect;
        private Vector3 rotation;
        private bool camerabewegen;
        private float cameraspeed = 0.1f;

        #endregion

        public Game1()
        {
            ME = this;
            graphics = new GraphicsDeviceManager(this)
            {
                GraphicsProfile = GraphicsProfile.HiDef,
                PreferredBackBufferWidth = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width,
                PreferredBackBufferHeight = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height,
                IsFullScreen = false,
                SynchronizeWithVerticalRetrace = false

            };
            IsFixedTimeStep = false;
            Window.IsBorderless = true;

            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            content = Content;
            IsMouseVisible = true;
            form = (System.Windows.Forms.Form)System.Windows.Forms.Control.FromHandle(this.Window.Handle);
            form.Location = new System.Drawing.Point(0, 0);
            //form.MaximizeBox = true;
            //form.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            //form.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            base.Initialize();
        }

        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            cameraeffect = new BasicEffect(GraphicsDevice);
            cameraeffect.Projection = camprojection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(90), GraphicsDevice.Viewport.AspectRatio, 0.1f, 1000000);
            cameraeffect.World = camworld = Matrix.Identity;

            //Inizialising Corner Positions of Skybox Cube
            Vector3 pos1 = new Vector3(0, 0, 0);
            Vector3 pos2 = new Vector3(0, 0, 1);
            Vector3 pos3 = new Vector3(1, 0, 1);
            Vector3 pos4 = new Vector3(1, 0, 0);
            Vector3 pos5 = new Vector3(0, 1, 0);
            Vector3 pos6 = new Vector3(0, 1, 1);
            Vector3 pos7 = new Vector3(1, 1, 1);
            Vector3 pos8 = new Vector3(1, 1, 0);

            //Adding Vertexes of Skybox Cube
            List<VertexPosition> vertexes = new List<VertexPosition>();
            vertexes.AddRange(new[] { new VertexPosition(pos1), new VertexPosition(pos2), new VertexPosition(pos6), new VertexPosition(pos1), new VertexPosition(pos6), new VertexPosition(pos5) });
            vertexes.AddRange(new[] { new VertexPosition(pos2), new VertexPosition(pos3), new VertexPosition(pos7), new VertexPosition(pos2), new VertexPosition(pos7), new VertexPosition(pos6) });
            vertexes.AddRange(new[] { new VertexPosition(pos3), new VertexPosition(pos4), new VertexPosition(pos8), new VertexPosition(pos3), new VertexPosition(pos8), new VertexPosition(pos7) });
            vertexes.AddRange(new[] { new VertexPosition(pos1), new VertexPosition(pos5), new VertexPosition(pos8), new VertexPosition(pos1), new VertexPosition(pos8), new VertexPosition(pos4) });
            vertexes.AddRange(new[] { new VertexPosition(pos7), new VertexPosition(pos5), new VertexPosition(pos6), new VertexPosition(pos5), new VertexPosition(pos7), new VertexPosition(pos8) });
            vertexes.AddRange(new[] { new VertexPosition(pos1), new VertexPosition(pos3), new VertexPosition(pos2), new VertexPosition(pos3), new VertexPosition(pos1), new VertexPosition(pos4) });

            cloudbox = new VertexBuffer(GraphicsDevice, VertexPosition.VertexDeclaration, vertexes.Count, BufferUsage.WriteOnly);
            cloudbox.SetData(vertexes.ToArray());

            cloud_effect = Content.Load<Effect>("cloud_effect");
            maintarget = new RenderTarget2D(GraphicsDevice, Screenwidth / 2, Screenheight / 2, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8);
            BoundingBox box = new BoundingBox(new Vector3(0, 500, 0), new Vector3(10000, 1000, 10000));
            boxworld = Matrix.CreateScale(box.Max.X, box.Max.Y, box.Max.Z) * Matrix.CreateTranslation(box.Min);
            cloud_effect.Parameters["b_min"].SetValue(box.Min);
            cloud_effect.Parameters["b_max"].SetValue(box.Min + box.Max);

            float[] data = new float[255 * 255 * 255];
            Parallel.For(0, 255, x =>
            //for (int x = 0; x < 255; ++x)
            {
                for (int y = 0; y < 255; ++y)
                {
                    for (int z = 0; z < 255; ++z)
                    {
                        data[x + y * 255 + z * 255 * 255] = perlin.speedperlin3D(x + 0.1f, y + 0.1f, z + 0.1f);
                    }
                }
            });
            tex = new Texture3D(GraphicsDevice, 255, 255, 255, false, SurfaceFormat.Single);
            tex.SetData(data);
            cloud_effect.Parameters["perlintex"].SetValue(tex);
            cloud_effect.Parameters["bgcolor"].SetValue(Color.CornflowerBlue.ToVector3());
        }

        protected override void Update(GameTime gameTime)
        {
            keyboardstate = Keyboard.GetState();
            var mousePosition = new Vector2(System.Windows.Forms.Cursor.Position.X, System.Windows.Forms.Cursor.Position.Y);
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            if (keyboardstate.IsKeyDown(Keys.Left))
                SunRot += 0.01f;
            if (keyboardstate.IsKeyDown(Keys.Right))
                SunRot -= 0.01f;
            LightDir = new Vector3((float)Math.Sin(SunRot), (float)Math.Cos(SunRot), 0);

            BoundingBox box = new BoundingBox(new Vector3(camerapos.X - 10000, 500, camerapos.Z - 10000), new Vector3(20000, 1500, 20000));
            boxworld = Matrix.CreateScale(box.Max.X, box.Max.Y, box.Max.Z) * Matrix.CreateTranslation(box.Min);
            cloud_effect.Parameters["b_min"].SetValue(box.Min);
            cloud_effect.Parameters["b_max"].SetValue(box.Min + box.Max);





            #region Updating Camera
            cameraspeed = Keyboard.GetState().IsKeyDown(Keys.LeftShift) ? 15.0f : 1.5f;
            if (Keyboard.GetState().IsKeyDown(Keys.A))
            {
                camerapos.Z -= (float)Math.Sin(rotation.X) * 2 * cameraspeed;
                camerapos.X += (float)Math.Cos(rotation.X) * 2 * cameraspeed;
            }
            if (Keyboard.GetState().IsKeyDown(Keys.D))
            {
                camerapos.Z += (float)Math.Sin(rotation.X) * 2 * cameraspeed;
                camerapos.X -= (float)Math.Cos(rotation.X) * 2 * cameraspeed;
            }
            if (Keyboard.GetState().IsKeyDown(Keys.Space))
                camerapos.Y += 2 * cameraspeed;
            if (Keyboard.GetState().IsKeyDown(Keys.LeftControl))
                camerapos.Y -= 2 * cameraspeed;

            if (keyboardstate.IsKeyDown(Keys.Tab) && !oldkeyboardstate.IsKeyDown(Keys.Tab))
            {
                Mouse.SetPosition(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2);
                camerabewegen = !camerabewegen;
            }
            if (camerabewegen == true && this.IsActive)
            {
                int changed = 0;
                float deltax, deltay;
                deltax = System.Windows.Forms.Cursor.Position.X - cameramousepos.X;
                deltay = System.Windows.Forms.Cursor.Position.Y - cameramousepos.Y;
                mouserotationbuffer.X += 0.004f * deltax;
                mouserotationbuffer.Y += 0.004f * deltay;
                if (mouserotationbuffer.Y < MathHelper.ToRadians(-88))
                {
                    mouserotationbuffer.Y = mouserotationbuffer.Y - (mouserotationbuffer.Y - MathHelper.ToRadians(-88));
                }

                if (mouserotationbuffer.Y > MathHelper.ToRadians(88))
                {
                    mouserotationbuffer.Y = mouserotationbuffer.Y - (mouserotationbuffer.Y - MathHelper.ToRadians(88));
                }

                if (cameramousepos != mousePosition)
                    changed = 1;
                rotation = new Vector3(-mouserotationbuffer.X, -mouserotationbuffer.Y, 0);
                if (changed == 1)
                {
                    System.Windows.Forms.Cursor.Position = mousepointpos;
                }
            }

            if (Mouse.GetState().RightButton == ButtonState.Pressed && IsActive)
            {
                if (camerabewegen == false)
                {
                    camerabewegen = true;
                    cameramousepos = mousePosition;
                    mousepointpos.X = (int)mousePosition.X;
                    mousepointpos.Y = (int)mousePosition.Y;
                }
            }

            if (Mouse.GetState().RightButton == ButtonState.Released && camerabewegen == true)
            {
                camerabewegen = false;
            }


            Matrix rotationMatrix = Matrix.CreateRotationY(rotation.X); // * Matrix.CreateRotationX(rotationY);
            Vector3 transformedReference = Vector3.TransformNormal(new Vector3(0, 0, 1000), rotationMatrix);
            Vector3 cameraLookat = camerapos + transformedReference;
            camerarichtung.Y = cameraLookat.Y - (float)Math.Sin(-rotation.Y) * Vector3.Distance(camerapos, cameraLookat);
            camerarichtung.X = cameraLookat.X - (cameraLookat.X - camerapos.X) * (float)(1 - Math.Cos(rotation.Y));
            camerarichtung.Z = cameraLookat.Z - (cameraLookat.Z - camerapos.Z) * (float)(1 - Math.Cos(rotation.Y));
            if (keyboardstate.IsKeyUp(Keys.LeftAlt))
            {
                if (Keyboard.GetState().IsKeyDown(Keys.W))
                {
                    var camerablickrichtung = camerapos - camerarichtung;
                    camerablickrichtung = camerablickrichtung / camerablickrichtung.Length();
                    camerapos -= camerablickrichtung * 2 * cameraspeed;
                    camerarichtung -= camerablickrichtung * 2 * cameraspeed;
                }

                if (Keyboard.GetState().IsKeyDown(Keys.S))
                {
                    var camerablickrichtung = camerapos - camerarichtung;
                    camerablickrichtung = camerablickrichtung / camerablickrichtung.Length();
                    camerapos += camerablickrichtung * 2 * cameraspeed;
                    camerarichtung += camerablickrichtung * 2 * cameraspeed;
                }
            }

            cameraeffect.View = Matrix.CreateLookAt(camerapos, camerarichtung, Vector3.Up);
            camworld = cameraeffect.World;
            camview = cameraeffect.View;
            camprojection = cameraeffect.Projection;
            #endregion




            currenttime += 0.15f;
            cloud_effect.Parameters["currenttime"].SetValue(currenttime);
            cloud_effect.Parameters["LightDir"].SetValue(LightDir);

            base.Update(gameTime);

            oldkeyboardstate = keyboardstate;
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // Draw Box Wire-frame

            GraphicsDevice.SetRenderTarget(maintarget);
            GraphicsDevice.Clear(Color.CornflowerBlue);
            GraphicsDevice.SetVertexBuffer(cloudbox);
            cloud_effect.Parameters["World"].SetValue(boxworld);
            cloud_effect.Parameters["WorldViewProjection"].SetValue(boxworld * camview * camprojection);
            cloud_effect.Parameters["EyePosition"].SetValue(camerapos);
            cloud_effect.CurrentTechnique.Passes[0].Apply();
            GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, cloudbox.VertexCount / 3);


            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.Green);

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);
            spriteBatch.Draw(maintarget, Vector2.Zero, null, Color.White, 0, Vector2.Zero, 2, SpriteEffects.None, 0);
            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
