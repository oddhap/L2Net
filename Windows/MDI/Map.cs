using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Collections;
using System.Windows.Forms;
using System.IO;

namespace L2_login
{
    public class Map : Base
    {
        private ArrayList cache_draw = new ArrayList();
        private ArrayList tmp_players = new ArrayList();
        private ArrayList tmp_npcs = new ArrayList();
        private ArrayList tmp_items = new ArrayList();
        private ArrayList tmp_path = new ArrayList();
        private ArrayList tmp_walls = new ArrayList();

        private Graphics dxGraphics;
        private Bitmap backBuffer;
        private bool resources_created = false;

        private const int wid = 200;
        private const int wid_2 = wid / 2;
        private const int hgt = 16;

        private int last_MAPX = -1000;
        private int last_MAPY = -1000;
        private int last_MAPZ = -10000000;

        private ArrayList maps = new ArrayList();

        private int dx, dy, dr, dr2;
        private int xc;
        private int yc;
        private int zc;
        private int xm;
        private int ym;
        private float scale;

        private DrawData ddt;
        private string ddtext;

        private uint my_target;
        private float my_z;
        private float zrange_draw;

        public static Pen BlackPen = new Pen(Color.Black);
        public static Pen WhitePen = new Pen(Color.White);
        public static Pen BluePen = new Pen(Color.Blue);
        public static Pen DBluePen = new Pen(Color.DarkBlue);
        public static Pen LBluePen = new Pen(Color.LightBlue);
        public static Pen RedPen = new Pen(Color.Red);
        public static Pen YellowPen = new Pen(Color.Yellow);
        public static Pen GreenPen = new Pen(Color.DarkGreen);
        public static Pen PurplePen = new Pen(Color.FromArgb(184, 0, 184));
        public static Pen LPurplePen = new Pen(Color.FromArgb(247, 0, 247));

        public static SolidBrush BlackBrush = new SolidBrush(Color.Black);
        public static SolidBrush WhiteBrush = new SolidBrush(Color.White);
        public static SolidBrush BlueBrush = new SolidBrush(Color.Blue);
        public static SolidBrush DBlueBrush = new SolidBrush(Color.DarkBlue);
        public static SolidBrush LBlueBrush = new SolidBrush(Color.LightBlue);
        public static SolidBrush RedBrush = new SolidBrush(Color.Red);
        public static SolidBrush YellowBrush = new SolidBrush(Color.Yellow);
        public static SolidBrush GreenBrush = new SolidBrush(Color.DarkGreen);
        public static SolidBrush PurpleBrush = new SolidBrush(Color.FromArgb(184, 0, 184));
        public static SolidBrush LPurpleBrush = new SolidBrush(Color.FromArgb(247, 0, 247));

        public static Color text_color;
        public static Color text_color_shadow;

        public static System.Drawing.Font drawFont = new System.Drawing.Font("Arial", 10);

        private volatile bool LoadTextures = false;
        private DateTime LastTextureLoad = new DateTime(0L);
        private volatile bool resized = false;

        public PictureBox pictureBox_test;

        public Map(Form pf)
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.None;
            this.pictureBox_test.MouseDown += new MouseEventHandler(Form1_MouseDown);
            this.MdiParent = pf;
            this.MdiParent.Resize += new EventHandler(MdiParent_Resize);
            MdiParent_Resize(null, null);
            LoadMiniMap();
            Init_GDI();
        }

        private void Init_GDI()
        {
            CreateBackBuffer();
            this.Resize += new EventHandler(Map_Resize);
        }

        private void CreateBackBuffer()
        {
            if (backBuffer != null)
                backBuffer.Dispose();
            if (dxGraphics != null)
                dxGraphics.Dispose();

            int width = Math.Max(this.Width, 100);
            int height = Math.Max(this.Height, 100);
            backBuffer = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            dxGraphics = Graphics.FromImage(backBuffer);
            dxGraphics.SmoothingMode = SmoothingMode.AntiAlias;
            dxGraphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            resources_created = true;
            LastTextureLoad = DateTime.Now.AddMilliseconds(500);
            LoadTextures = true;
        }

        private void UnloadResources()
        {
            resources_created = false;
            if (backBuffer != null)
            {
                backBuffer.Dispose();
                backBuffer = null;
            }
            if (dxGraphics != null)
            {
                dxGraphics.Dispose();
                dxGraphics = null;
            }

            foreach (MapData map in maps)
            {
                try
                {
                    if (map.dxTexture != null)
                    {
                        map.dxTexture.Dispose();
                        map.dxTexture = null;
                    }
                }
                catch { }
            }
        }

        void Map_Resize(object sender, EventArgs e)
        {
            resized = true;
        }

        protected override void Dispose(bool disposing)
        {
            this.MdiParent.Resize -= new EventHandler(MdiParent_Resize);
            UnloadResources();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code
        private void InitializeComponent()
        {
            this.pictureBox_test = new PictureBox();
            ((System.ComponentModel.ISupportInitialize)this.pictureBox_test).BeginInit();
            this.SuspendLayout();
            this.pictureBox_test.Dock = DockStyle.Fill;
            this.pictureBox_test.Location = new System.Drawing.Point(0, 0);
            this.pictureBox_test.Name = "pictureBox_test";
            this.pictureBox_test.Size = new Size(622, 518);
            this.pictureBox_test.TabIndex = 0;
            this.pictureBox_test.TabStop = false;
            this.AutoScaleBaseSize = new Size(5, 13);
            this.AutoValidate = AutoValidate.EnableAllowFocusChange;
            this.ClientSize = new Size(622, 518);
            this.Controls.Add(this.pictureBox_test);
            this.Name = "Map";
            this.Text = "Map";
            ((System.ComponentModel.ISupportInitialize)this.pictureBox_test).EndInit();
            this.ResumeLayout(false);
        }
        #endregion

        public void Draw()
        {
            this.Invalidate(new Region(new Rectangle(0, 0, this.Width, this.Height)));
        }

        protected override void OnPaintBackground(PaintEventArgs prevent) { }

        protected override void OnPrint(PaintEventArgs e) { }

        protected override void OnPaint(PaintEventArgs e)
        {
            try
            {
                if (this.Width > 100 && this.Height > 100)
                {
                    if (resized)
                    {
                        resized = false;
                        UnloadResources();
                        CreateBackBuffer();
                        LastTextureLoad = DateTime.Now.AddMilliseconds(500);
                        LoadTextures = true;
                    }

                    ClearUnusedMaps();

                    if (!resources_created)
                    {
                        CreateBackBuffer();
                    }

                    if (resources_created)
                    {
                        DrawGame();
                    }
                }
            }
            catch { }
        }

        protected void DrawGame()
        {
            dxGraphics.Clear(Color.White);
            DrawGameInner();

            pictureBox_test.Image = (Bitmap)backBuffer.Clone();
            pictureBox_test.Refresh();
        }

        protected void DrawGameInner()
        {
            if (LoadTextures)
            {
                try
                {
                    LoadTexturesInternal();
                }
                catch
                {
#if DEBUG
                    Globals.l2net_home.Add_OnlyDebug("failed to load textures internal");
#endif
                }
            }

            my_target = Globals.gamedata.my_char.TargetID;
            my_z = Globals.gamedata.my_char.Z;
            zrange_draw = Math.Abs((float)Util.GetInt32(Globals.l2net_home.textBox_zrange_map.Text));

            xc = Util.Float_Int32(Globals.gamedata.my_char.X);
            yc = Util.Float_Int32(Globals.gamedata.my_char.Y);
            zc = Util.Float_Int32(Globals.gamedata.my_char.Z);

            xm = this.Width / 2;
            ym = this.Height / 2;

            scale = MapThread.GetZoom();

            tmp_path.Clear();
            tmp_walls.Clear();
            cache_draw.Clear();

            try
            {
                if (Globals.gamedata.my_pet.ID != 0)
                {
                    ddt = new DrawData();
                    ddt.ID = Globals.gamedata.my_pet.ID;
                    ddt.X = Util.Float_Int32(Globals.gamedata.my_pet.X);
                    ddt.Y = Util.Float_Int32(Globals.gamedata.my_pet.Y);
                    ddt.Radius = Globals.gamedata.my_pet.CollisionRadius;
                    ddt.Text = Globals.ShowNamesPcs ? Globals.gamedata.my_pet.Name : "";
                    ddt.Color1 = 5;
                    ddt.Color2 = Globals.gamedata.my_pet.Karma > 0 ? 0 :
                                  (Globals.gamedata.my_pet.PvPFlag == 1 ? 1 :
                                  (Globals.gamedata.my_pet.PvPFlag == 2 ? 2 : 3));
                    cache_draw.Add(ddt);
                }
                if (Globals.gamedata.my_pet1.ID != 0)
                {
                    ddt = new DrawData();
                    ddt.ID = Globals.gamedata.my_pet1.ID;
                    ddt.X = Util.Float_Int32(Globals.gamedata.my_pet1.X);
                    ddt.Y = Util.Float_Int32(Globals.gamedata.my_pet1.Y);
                    ddt.Radius = Globals.gamedata.my_pet1.CollisionRadius;
                    ddt.Text = Globals.ShowNamesPcs ? Globals.gamedata.my_pet1.Name : "";
                    ddt.Color1 = 5;
                    ddt.Color2 = Globals.gamedata.my_pet1.Karma > 0 ? 0 :
                                  (Globals.gamedata.my_pet1.PvPFlag == 1 ? 1 :
                                  (Globals.gamedata.my_pet1.PvPFlag == 2 ? 2 : 3));
                    cache_draw.Add(ddt);
                }
                if (Globals.gamedata.my_pet2.ID != 0)
                {
                    ddt = new DrawData();
                    ddt.ID = Globals.gamedata.my_pet2.ID;
                    ddt.X = Util.Float_Int32(Globals.gamedata.my_pet2.X);
                    ddt.Y = Util.Float_Int32(Globals.gamedata.my_pet2.Y);
                    ddt.Radius = Globals.gamedata.my_pet2.CollisionRadius;
                    ddt.Text = Globals.ShowNamesPcs ? Globals.gamedata.my_pet2.Name : "";
                    ddt.Color1 = 5;
                    ddt.Color2 = Globals.gamedata.my_pet2.Karma > 0 ? 0 :
                                  (Globals.gamedata.my_pet2.PvPFlag == 1 ? 1 :
                                  (Globals.gamedata.my_pet2.PvPFlag == 2 ? 2 : 3));
                    cache_draw.Add(ddt);
                }
                if (Globals.gamedata.my_pet3.ID != 0)
                {
                    ddt = new DrawData();
                    ddt.ID = Globals.gamedata.my_pet3.ID;
                    ddt.X = Util.Float_Int32(Globals.gamedata.my_pet3.X);
                    ddt.Y = Util.Float_Int32(Globals.gamedata.my_pet3.Y);
                    ddt.Radius = Globals.gamedata.my_pet3.CollisionRadius;
                    ddt.Text = Globals.ShowNamesPcs ? Globals.gamedata.my_pet3.Name : "";
                    ddt.Color1 = 5;
                    ddt.Color2 = Globals.gamedata.my_pet3.Karma > 0 ? 0 :
                                  (Globals.gamedata.my_pet3.PvPFlag == 1 ? 1 :
                                  (Globals.gamedata.my_pet3.PvPFlag == 2 ? 2 : 3));
                    cache_draw.Add(ddt);
                }
            }
            catch { }

            try
            {
                SortedList tmp_party = new SortedList();
                if (Globals.PartyLock.TryEnterReadLock(Globals.THREAD_WAIT_DX))
                {
                    try
                    {
                        foreach (uint key in Globals.gamedata.PartyMembers.Keys)
                            tmp_party.Add(key, key);
                    }
                    finally { Globals.PartyLock.ExitReadLock(); }
                }

                if (Globals.PlayerLock.TryEnterReadLock(Globals.THREAD_WAIT_DX))
                {
                    try
                    {
                        foreach (CharInfo player in Globals.gamedata.nearby_chars.Values)
                        {
                            if (Math.Abs(player.Z - my_z) <= zrange_draw)
                            {
                                ddt = new DrawData();
                                ddt.ID = player.ID;
                                ddt.X = Util.Float_Int32(player.X);
                                ddt.Y = Util.Float_Int32(player.Y);
                                ddt.Radius = player.CollisionRadius;
                                ddt.Text = Globals.ShowNamesPcs ? player.Name : "";
                                ddt.Color1 = tmp_party.ContainsKey(player.ID) ? 5 : 2;
                                ddt.Color2 = ((player.Karma > 0) && (Globals.gamedata.Chron <= Chronicle.CT2_6)) ? 0 :
                                            ((player.Karma < 0) && (Globals.gamedata.Chron >= Chronicle.CT3_0)) ? 0 :
                                            (player.PvPFlag == 1 ? 1 : (player.PvPFlag == 2 ? 2 : 3));
                                cache_draw.Add(ddt);
                            }
                        }
                    }
                    finally { Globals.PlayerLock.ExitReadLock(); }
                }
            }
            catch { }

            try
            {
                if (Globals.NPCLock.TryEnterReadLock(Globals.THREAD_WAIT_DX))
                {
                    try
                    {
                        foreach (NPCInfo npc in Globals.gamedata.nearby_npcs.Values)
                        {
                            if (Math.Abs(npc.Z - my_z) <= zrange_draw && npc.isInvisible != 1)
                            {
                                ddt = new DrawData();
                                ddt.ID = npc.ID;
                                ddt.X = Util.Float_Int32(npc.X);
                                ddt.Y = Util.Float_Int32(npc.Y);
                                ddt.Radius = npc.CollisionRadius;
                                ddt.Text = Globals.ShowNamesNpcs ? Util.GetNPCName(npc.NPCID) : "";
                                ddt.Color1 = npc.isAttackable == 0 ? 3 : 0;
                                ddt.Color2 = 3;
                                cache_draw.Add(ddt);
                            }
                        }
                    }
                    finally { Globals.NPCLock.ExitReadLock(); }
                }
            }
            catch { }

            try
            {
                if (Globals.ItemLock.TryEnterReadLock(Globals.THREAD_WAIT_DX))
                {
                    try
                    {
                        foreach (ItemInfo item in Globals.gamedata.nearby_items.Values)
                        {
                            if (Math.Abs(item.Z - my_z) <= zrange_draw)
                            {
                                ddt = new DrawData();
                                ddt.ID = item.ID;
                                ddt.X = Util.Float_Int32(item.X);
                                ddt.Y = Util.Float_Int32(item.Y);
                                ddt.Radius = item.DropRadius;
                                ddt.Text = Globals.ShowNamesItems ? Util.GetItemName(item.ItemID) : "";
                                ddt.Color1 = 1;
                                ddt.Color2 = 3;
                                cache_draw.Add(ddt);
                            }
                        }
                    }
                    finally { Globals.ItemLock.ExitReadLock(); }
                }
            }
            catch { }

            try
            {
                if (Globals.l2net_home.checkBox_minimap.Checked)
                {
                    int x_block = (int)((Globals.gamedata.my_char.X + Globals.ModX) / Globals.UNITS);
                    int y_block = (int)((Globals.gamedata.my_char.Y + Globals.ModY) / Globals.UNITS);
                    int z_diff = (int)Math.Abs(Globals.gamedata.my_char.Z - last_MAPZ);

                    if (x_block != last_MAPX || y_block != last_MAPY || z_diff >= Globals.ZRANGE_DIFF)
                    {
                        last_MAPX = x_block;
                        last_MAPY = y_block;
                        last_MAPZ = (int)Globals.gamedata.my_char.Z;

                        switch (Globals.ViewRange)
                        {
                            case 1:
                                LoadMapFile(last_MAPX, last_MAPY, last_MAPZ);
                                LoadMapFile(last_MAPX - 1, last_MAPY, last_MAPZ);
                                LoadMapFile(last_MAPX + 1, last_MAPY, last_MAPZ);
                                LoadMapFile(last_MAPX, last_MAPY - 1, last_MAPZ);
                                LoadMapFile(last_MAPX, last_MAPY + 1, last_MAPZ);
                                break;
                            case 2:
                                for (int i = -1; i <= 1; i++)
                                    for (int j = -1; j <= 1; j++)
                                        LoadMapFile(last_MAPX + i, last_MAPY + j, last_MAPZ);
                                break;
                            default:
                                LoadMapFile(last_MAPX, last_MAPY, last_MAPZ);
                                break;
                        }
                    }

                    switch (Globals.ViewRange)
                    {
                        case 1:
                            DrawMap(last_MAPX, last_MAPY, last_MAPZ);
                            DrawMap(last_MAPX - 1, last_MAPY, last_MAPZ);
                            DrawMap(last_MAPX + 1, last_MAPY, last_MAPZ);
                            DrawMap(last_MAPX, last_MAPY - 1, last_MAPZ);
                            DrawMap(last_MAPX, last_MAPY + 1, last_MAPZ);
                            break;
                        case 2:
                            for (int i = -1; i <= 1; i++)
                                for (int j = -1; j <= 1; j++)
                                    DrawMap(last_MAPX + i, last_MAPY + j, last_MAPZ);
                            break;
                        default:
                            DrawMap(last_MAPX, last_MAPY, last_MAPZ);
                            break;
                    }

                    if (my_target == Globals.gamedata.my_char.ID)
                        DrawFilledBox(xm - 5, ym - 5, xm + 5, ym + 5, Color.Red);
                    else
                        DrawBox(xm - 5, ym - 5, xm + 5, ym + 5, Color.Red);
                }
            }
            catch { }

            try
            {
                if (Globals.gamedata.Paths.PointList.Count > 0)
                {
                    for (int i = 0; i < Globals.gamedata.Paths.PointList.Count; i++)
                    {
                        Point npt = new Point();
                        npt.X = ((Point)Globals.gamedata.Paths.PointList[i]).X;
                        npt.Y = ((Point)Globals.gamedata.Paths.PointList[i]).Y;
                        npt.X = GetScaledX(npt.X);
                        npt.Y = GetScaledY(npt.Y);
                        DrawBox((int)npt.X - 2, (int)npt.Y - 2, (int)npt.X + 2, (int)npt.Y + 2, Color.Black);
                        tmp_path.Add(npt);
                    }
                    if (Globals.gamedata.Paths.PointList.Count > 1)
                    {
                        Point p1 = (Point)tmp_path[0];
                        Point p2 = (Point)tmp_path[tmp_path.Count - 1];
                        DrawLine((int)p1.X, (int)p1.Y, (int)p2.X, (int)p2.Y, Color.Black);
                        for (int pi = 1; pi < tmp_path.Count; pi++)
                        {
                            p1 = (Point)tmp_path[pi - 1];
                            p2 = (Point)tmp_path[pi];
                            DrawLine((int)p1.X, (int)p1.Y, (int)p2.X, (int)p2.Y, Color.Black);
                        }
                    }
                }

                for (int i = 0; i < Globals.gamedata.Walls.Count; i++)
                {
                    Wall tmp_w = new Wall();
                    Point npt1 = new Point();
                    Point npt2 = new Point();
                    npt1.X = ((Wall)Globals.gamedata.Walls[i]).P1.X;
                    npt1.Y = ((Wall)Globals.gamedata.Walls[i]).P1.Y;
                    npt1.X = GetScaledX(npt1.X);
                    npt1.Y = GetScaledY(npt1.Y);
                    npt2.X = ((Wall)Globals.gamedata.Walls[i]).P2.X;
                    npt2.Y = ((Wall)Globals.gamedata.Walls[i]).P2.Y;
                    npt2.X = GetScaledX(npt2.X);
                    npt2.Y = GetScaledY(npt2.Y);
                    tmp_w.P1 = npt1; tmp_w.P2 = npt2;
                    tmp_walls.Add(tmp_w);
                }
                for (int pi = 0; pi < tmp_walls.Count; pi++)
                {
                    Point p1 = ((Wall)tmp_walls[pi]).P1;
                    Point p2 = ((Wall)tmp_walls[pi]).P2;
                    DrawLine((int)p1.X, (int)p1.Y, (int)p2.X, (int)p2.Y, Color.MediumVioletRed);
                }
            }
            catch { }

            try
            {
                text_color = Globals.White_Names ? Color.White : Color.Black;
                text_color_shadow = Globals.White_Names ? Color.Black : Color.Gray;

                for (int i = 0; i < cache_draw.Count; i++)
                {
                    dx = GetScaledX(((DrawData)cache_draw[i]).X);
                    dy = GetScaledY(((DrawData)cache_draw[i]).Y);
                    dr2 = (int)(((DrawData)cache_draw[i]).Radius / scale);
                    dr = dr2 / 2;
                    if (dr2 < Globals.MIN_RADIUS)
                    {
                        dr2 = Globals.MIN_RADIUS;
                        dr = dr2 / 2;
                    }

                    bool isTarget = (((DrawData)cache_draw[i]).ID == my_target);
                    Color fillCol = GetColor1(((DrawData)cache_draw[i]).Color1);
                    Color outlineCol = GetColor1(((DrawData)cache_draw[i]).Color1);

                    if (isTarget)
                        DrawFilledBox(dx - dr, dy - dr, dx + dr, dy + dr, fillCol);
                    else
                        DrawBox(dx - dr, dy - dr, dx + dr, dy + dr, outlineCol);

                    if (((DrawData)cache_draw[i]).Text.Length > 0)
                    {
                        ddtext = ((DrawData)cache_draw[i]).Text;
                        Color textCol = GetColor2(((DrawData)cache_draw[i]).Color2);
                        DrawText(ddtext, dx - wid_2, dy - hgt - 10, wid, hgt, textCol);
                    }
                }
            }
            catch { }
        }

        private void DrawMap(int x_block, int y_block, int z)
        {
            MapData map = GetMapFile(x_block, y_block, z);
            if (map == null || map.dxTexture == null)
                return;

            int lx = GetScaledX(map.UpperX);
            int ly = GetScaledY(map.UpperY);
            int mx = GetScaledX(map.LowerX);
            int my = GetScaledY(map.LowerY);

            if ((mx < 0) || (my < 0) || (lx > Width) || (ly > Height))
                return;

            int destX = Math.Max(0, lx);
            int destY = Math.Max(0, ly);
            int srcX = Math.Max(0, -lx);
            int srcY = Math.Max(0, -ly);
            int drawW = Math.Min(mx - lx, Width - lx);
            int drawH = Math.Min(my - ly, Height - ly);

            if (drawW > 0 && drawH > 0 && srcX < map.dxTexture.Width && srcY < map.dxTexture.Height)
            {
                Rectangle srcRect = new Rectangle(srcX, srcY, Math.Min(drawW, map.dxTexture.Width - srcX), Math.Min(drawH, map.dxTexture.Height - srcY));
                dxGraphics.DrawImage(map.dxTexture, destX, destY, srcRect, GraphicsUnit.Pixel);
            }
        }

        private void DrawFilledBox(int x1, int y1, int x2, int y2, Color col)
        {
            int rx = Math.Min(x1, x2);
            int ry = Math.Min(y1, y2);
            int rw = Math.Abs(x2 - x1);
            int rh = Math.Abs(y2 - y1);
            using (SolidBrush br = new SolidBrush(col))
                dxGraphics.FillRectangle(br, rx, ry, rw, rh);
        }

        private void DrawBox(int x1, int y1, int x2, int y2, Color col)
        {
            using (Pen pen = new Pen(col))
            {
                dxGraphics.DrawRectangle(pen, Math.Min(x1, x2), Math.Min(y1, y2), Math.Abs(x2 - x1), Math.Abs(y2 - y1));
            }
        }

        private void DrawLine(int x1, int y1, int x2, int y2, Color col)
        {
            using (Pen pen = new Pen(col))
            {
                dxGraphics.DrawLine(pen, x1, y1, x2, y2);
            }
        }

        private void DrawText(string text, int x, int y, int w, int h, Color col)
        {
            using (Font font = new Font("Arial", 10))
            using (SolidBrush brush = new SolidBrush(col))
            {
                StringFormat sf = new StringFormat();
                sf.Alignment = StringAlignment.Center;
                RectangleF rect = new RectangleF(x, y, w, h);
                dxGraphics.DrawString(text, font, brush, rect, sf);
            }
        }

        private Color GetColor1(int c)
        {
            switch (c)
            {
                case 0: return Color.Black;
                case 1: return Color.Yellow;
                case 2: return Color.Blue;
                case 3: return Color.SkyBlue;
                case 4: return Color.Red;
                case 5: return Color.OrangeRed;
                default: return Color.Black;
            }
        }

        private Color GetColor2(int c)
        {
            switch (c)
            {
                case 0: return Color.Red;
                case 1: return Color.FromArgb(184, 0, 184);
                case 2: return Color.FromArgb(247, 0, 247);
                case 3: return text_color_shadow;
                default: return text_color;
            }
        }

        private int GetScaledX(float x) { return (int)((x - xc) / scale) + xm; }
        private int GetScaledX(double x) { return (int)((x - xc) / scale) + xm; }
        private int GetScaledY(float y) { return (int)((y - yc) / scale) + ym; }
        private int GetScaledY(double y) { return (int)((y - yc) / scale) + ym; }

        private bool InDrawSpace(MouseEventArgs e)
        {
            return e.X > this.Left && e.X < this.Width + this.Left &&
                   e.Y > this.Top && e.Y < this.Top + this.Height;
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (!Globals.gamedata.logged_in || !Globals.gamedata.running || !InDrawSpace(e))
                return;

            if (e.Button == MouseButtons.XButton1)
            {
                if (Globals.l2net_home.trackBar_map_zoom.Value + 1 <= Globals.l2net_home.trackBar_map_zoom.Maximum)
                    Globals.l2net_home.trackBar_map_zoom.Value++;
            }
            if (e.Button == MouseButtons.XButton2)
            {
                if (Globals.l2net_home.trackBar_map_zoom.Value - 1 >= Globals.l2net_home.trackBar_map_zoom.Minimum)
                    Globals.l2net_home.trackBar_map_zoom.Value--;
            }
            if (e.Button == MouseButtons.Left)
            {
                int xc = Util.Float_Int32(Globals.gamedata.my_char.X);
                int yc = Util.Float_Int32(Globals.gamedata.my_char.Y);
                int zc = Util.Float_Int32(Globals.gamedata.my_char.Z);
                int Width = this.pictureBox_test.Width;
                int Height = this.pictureBox_test.Height;
                int xm = Width / 2;
                int ym = Height / 2;
                int mx = e.X - this.Left;
                int my = e.Y - this.Top;
                float mouse_scale = MapThread.GetZoom();
                int dx = (int)((mx - xm) * mouse_scale) + xc;
                int dy = (int)((my - ym) * mouse_scale) + yc;
                float radius;
                int minR = (int)(Globals.MIN_RADIUS * mouse_scale);

                if (Globals.gamedata.Paths.PointList.Count > 0)
                {
                    try
                    {
                        for (int i = 0; i < Globals.gamedata.Paths.PointList.Count; i++)
                        {
                            Point npt = new Point();
                            npt.X = ((Point)Globals.gamedata.Paths.PointList[i]).X;
                            npt.Y = ((Point)Globals.gamedata.Paths.PointList[i]).Y;
                            if (Math.Abs(npt.X - dx) < minR && Math.Abs(npt.Y - dy) < minR)
                            {
                                if (!Globals.gamedata.AddPolygon)
                                {
                                    Globals.l2net_home.Add_Text("Bounding polygon point selected, please select a new position", Globals.Green, TextType.BOT);
                                    Globals.gamedata.PointClicked = true;
                                    Globals.gamedata.New_Point_i = i;
                                    return;
                                }
                                else
                                {
                                    Globals.gamedata.Paths.PointList.RemoveAt(i);
                                    return;
                                }
                            }
                        }
                    }
                    catch { }
                }

                if (Globals.gamedata.PointClicked)
                {
                    try
                    {
                        Point pt = new Point();
                        pt.X = dx;
                        pt.Y = dy;
                        Globals.gamedata.Paths.PointList[Globals.gamedata.New_Point_i] = pt;
                        Globals.gamedata.PointClicked = false;
                        return;
                    }
                    catch { }
                }

                if (Globals.gamedata.AddPolygon)
                {
                    try
                    {
                        Point pt = new Point();
                        pt.X = dx;
                        pt.Y = dy;
                        Globals.gamedata.Paths.PointList.Add(pt);
                        return;
                    }
                    catch { }
                }

                if (Globals.ItemLock.TryEnterReadLock(Globals.THREAD_WAIT_DX))
                {
                    try
                    {
                        foreach (ItemInfo item in Globals.gamedata.nearby_items.Values)
                        {
                            radius = item.DropRadius < minR ? minR : item.DropRadius;
                            if (Math.Abs(item.X - dx) < radius && Math.Abs(item.Y - dy) < radius)
                            {
                                ServerPackets.ClickItem(item.ID, Util.Float_Int32(item.X), Util.Float_Int32(item.Y), Util.Float_Int32(item.Z), Globals.gamedata.Shift);
                                return;
                            }
                        }
                    }
                    finally { Globals.ItemLock.ExitReadLock(); }
                }

                if (Globals.NPCLock.TryEnterReadLock(Globals.THREAD_WAIT_DX))
                {
                    try
                    {
                        foreach (NPCInfo npc in Globals.gamedata.nearby_npcs.Values)
                        {
                            if (npc.isInvisible != 1)
                            {
                                radius = npc.CollisionRadius < minR ? minR : npc.CollisionRadius;
                                if (Math.Abs(npc.X - dx) < radius && Math.Abs(npc.Y - dy) < radius)
                                {
                                    ServerPackets.ClickChar(npc.ID, Util.Float_Int32(npc.X), Util.Float_Int32(npc.Y), Util.Float_Int32(npc.Z), Globals.gamedata.Control, Globals.gamedata.Shift);
                                    return;
                                }
                            }
                        }
                    }
                    finally { Globals.NPCLock.ExitReadLock(); }
                }

                if (Globals.PlayerLock.TryEnterReadLock(Globals.THREAD_WAIT_DX))
                {
                    try
                    {
                        foreach (CharInfo player in Globals.gamedata.nearby_chars.Values)
                        {
                            radius = player.CollisionRadius < minR ? minR : player.CollisionRadius;
                            if (Math.Abs(player.X - dx) < radius && Math.Abs(player.Y - dy) < radius)
                            {
                                ServerPackets.ClickChar(player.ID, Util.Float_Int32(player.X), Util.Float_Int32(player.Y), Util.Float_Int32(player.Z), Globals.gamedata.Control, Globals.gamedata.Shift);
                                return;
                            }
                        }
                    }
                    finally { Globals.PlayerLock.ExitReadLock(); }
                }

                if (Globals.gamedata.my_pet.ID != 0)
                {
                    radius = Globals.gamedata.my_pet.CollisionRadius < minR ? minR : Globals.gamedata.my_pet.CollisionRadius;
                    if (Math.Abs(Globals.gamedata.my_pet.X - dx) < radius && Math.Abs(Globals.gamedata.my_pet.Y - dy) < radius)
                    {
                        ServerPackets.ClickChar(Globals.gamedata.my_pet.ID, Util.Float_Int32(Globals.gamedata.my_pet.X), Util.Float_Int32(Globals.gamedata.my_pet.Y), Util.Float_Int32(Globals.gamedata.my_pet.Z), Globals.gamedata.Control, Globals.gamedata.Shift);
                        return;
                    }
                }

                if (!Globals.gamedata.Shift)
                    ServerPackets.MoveToPacket(dx, dy, zc);
            }
        }

        #pragma warning disable CS0169
        private Bitmap pictureBoxTest_Image;
#pragma warning restore CS0169

        private void LoadMiniMap()
        {
            try
            {
                string loaded;
                StreamReader filein = new StreamReader(Globals.PATH + "\\data\\maps.txt");
                while ((loaded = filein.ReadLine()) != null)
                {
                    MapData mapdata = new MapData();
                    mapdata.Parse(loaded);
                    maps.Add(mapdata);
                }
            }
            catch
            {
                Globals.l2net_home.Add_PopUpError("failed to load data\\maps.txt");
            }
        }

        private void LoadMapFile(int x_block, int y_block, int z)
        {
            MapData map = GetMapFile(x_block, y_block, z);
            if (map != null && map.Image == null)
            {
                if (File.Exists(Globals.PATH + "\\Maps\\" + map.FileName))
                {
                    map.Image = new MemoryStream();
                    if (map.Encrypted)
                    {
                        byte[] data = AES.Decrypt(Globals.PATH + "\\Maps\\" + map.FileName, Globals.Map_Key, Globals.Map_Salt);
                        new Bitmap(new MemoryStream(data)).Save(map.Image, System.Drawing.Imaging.ImageFormat.Bmp);
                    }
                    else
                    {
                        new Bitmap(Globals.PATH + "\\Maps\\" + map.FileName).Save(map.Image, System.Drawing.Imaging.ImageFormat.Bmp);
                    }
                    LoadTextures = true;
                }
            }
        }

        private void ClearUnusedMaps()
        {
            foreach (MapData map in maps)
                map.ReleaseResources();
        }

        private MapData GetMapFile(int x_block, int y_block, int z)
        {
            foreach (MapData map in maps)
            {
                if (map.X == x_block && map.Y == y_block && map.Z_Min <= z && map.Z_Max >= z)
                    return map;
            }
            return null;
        }

        private void LoadTexturesInternal()
        {
            if ((DateTime.Now - LastTextureLoad).Ticks < Globals.SLEEP_TEXTURE)
                return;

            foreach (MapData map in maps)
            {
                if (map.Image != null && map.dxTexture == null)
                {
                    map.Image.Position = 0;
                    map.dxTexture = new Bitmap(map.Image);
                    LastTextureLoad = DateTime.Now;
                    return;
                }
            }
            LoadTextures = false;
        }
    }
}