using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using DG.Tweening;
using UnityEngine.UI;

/// <summary>
/// 游戏地图（最上层容器）
/// 
/// 仅可能使 Grids 纯粹
/// </summary>
public class Map : MonoBehaviour
{

    #region 对象池
    private static Dictionary<string, List<GameObject>> ObjectPool = new Dictionary<string, List<GameObject>>();
    public static void PushToPool(string key, GameObject obj)
    {
        if(ObjectPool.ContainsKey(key))
        {
            ObjectPool[key].Add(obj);
        }
        else
        {
            ObjectPool.Add(key, new List<GameObject>() { obj });
        }
    }
    public static GameObject PullFromPool(string key)
    {
        List<GameObject> list;
        if (ObjectPool.TryGetValue(key,out list) && list.Count > 0)
        {
            GameObject obj = list[0];
            list.RemoveAt(0);
            return obj;
        }
        return null;
    }

    /// <summary>
    /// 创建对象唯一接口
    /// </summary>
    /// <param name="prefab"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static GameObject CreateObject(string key,GameObject prefab)
    {
        GameObject go = PullFromPool(key);
        if (go != null)
        {
            go.SetActive(true);
            return go;
        } 
        go = GameObject.Instantiate(prefab);
        return go;
    }
    public static GameObject CreateObject(string key, GameObject prefab, int x, int y)
    {
        GameObject go = CreateObject(key, prefab);
        Vector3 pos = Map.grids.Grid2WorldPosition(x, y) + Tracker.TopLeftOffset;
        pos.z = -y * 0.02f;
        go.transform.position = pos;
        return go;
    }

    public static void RecycleObject(string key,GameObject obj)
    {
        if (obj == null)
            return;
        obj.SetActive(false);
        PushToPool(key, obj);
    }
    #endregion

    #region 静态字段、属性和方法
    private static Transform Transform;

    public const int Width = 80;
    public const int Height = 80;
    public static int area = Width * Height;

	public int numOfAI=16;

    public static Map ins;
    public static Rectangle rect;//地图矩形
    public static Rectangle bornRect;//出生的矩形（不包括Border以及去掉上面6行）
    public static Grids grids;
    public static List<Tracker> trackerList;//每次SetGround后按地盘排序
    public static Dictionary<string, Tracker> trackerHash;//用名字映射

    private static Vector3 HidePosition = new Vector3(1000f, 0f, 0f);

 //   private static Dictionary<string, int> ownerGroundCount = new Dictionary<string, int>();//根据名字找到其拥有的Ground数量
    private static SpriteRenderer[,] grounds;//ground对象的索引

    private static short TrackerOrder = 0;
    
    /// <summary>
    /// 根据名字找到Tracker
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static Tracker GetTracker(string name)
    {
        Tracker t;
        trackerHash.TryGetValue(name, out t);
        return t;
        /*
        foreach (Tracker p in trackerList)
        {
            if (p.name == name)
                return p;
        }
        return null;
         */
    }

    /// <summary>
    /// 设置地盘（唯一接口）
    /// </summary>
    public static void SetGround(int x, int y, Tracker tracker,bool born)
    {
        string currentOwnerName = grids[x, y].groundOwner;
        if (currentOwnerName != "")
        {
            grids[x, y].groundOwner = "";

            //数量-1
            Tracker p = GetTracker(currentOwnerName);
            p.ownGrounds.Remove(grids[x, y]);

            //删除显示对象
            GameObject groundGO = grounds[x, y].gameObject;
            Map.RecycleObject("Ground", groundGO);

            //如果地盘数量为0，则死亡
            if (!p.isDead && p.OwnGroundCount == 0)
            {
                tracker.KillOther(p);
            }
        }

        string name = tracker != null ? tracker.name : "";

        if (name != "")
        {
            grids[x, y].groundOwner = name;

            //数量+1
            tracker.ownGrounds.Add(grids[x, y]);
            
            //创建显示对象
            SpriteRenderer groundRenderer = CreateGround(x, y, tracker);
            grounds[x, y] = groundRenderer;

            //Fade
            if (!born)
            {
                SpriteRenderer renderer = groundRenderer.GetComponent<SpriteRenderer>();
                renderer.DOFade(0f, 0f);
                renderer.DOFade(1f, 0.5f);
            }

            //如果上面有Tracker 杀死它
            if (grids[x, y].trackerOn != "" && grids[x,y].trackerOn != name)
            {
                Tracker target = GetTracker(grids[x, y].trackerOn);
                tracker.KillOther(target);
            }
        }

        if (!born)
        {
            //更新排名
            trackerList.Sort(SortTracker);

        }
    }
    public static void ClearGround(int x, int y)
    {
        SetGround(x, y, null,false);
    }

    private static SpriteRenderer CreateGround(int x,int y,Tracker tracker)
    {
        GameObject ground = CreateObject(tracker.m_GroundPref.name, tracker.m_GroundPref);
        ground.transform.parent = tracker.Container;
        Vector3 pos = Map.grids.Grid2WorldPosition(x, y) + Tracker.TopLeftOffset;
        pos.z = -y * 0.02f;
        ground.transform.position = pos;
        SpriteRenderer r = ground.GetComponent<SpriteRenderer>();
        r.sprite = tracker.m_GroundSprite;
        r.sortingOrder = tracker.data.order;
        return r;
    }

    private static int SortTracker(Tracker a, Tracker b)
    {
        int ac = a.ownGrounds.Count;
        int bc = b.ownGrounds.Count;
        if (ac > bc)
            return -1;
        else if (bc > ac)
            return 1;
        else
            return 0;
    }
    #endregion

    #region Unity字段

    public GameObject m_BorderPref;
    public GameObject m_TrackerPref;
    public CameraFollow m_CameraFollow;

    public Tracker m_Player;
    public Image m_SkillMask;

    public event EventHandler OnRankSorted;

    public bool gameStarted = false;

    public Joystick joystick;
    #endregion

    static public Tracker humanPlayer;


    // Use this for initialization
    void Awake()
    {
        ins = this;
        Transform = transform;

        rect = new Rectangle(0, 0, Width, Height);
        bornRect = new Rectangle(1, 7, Width - 1, Height - 7);

        grids = new Grids(Width, Height);
        grounds = new SpriteRenderer[Height, Width];
        trackerList = new List<Tracker>();
        trackerHash = new Dictionary<string, Tracker>();

        joystick.OnTouchMove += OnJoystickMove;
 //       joystick.enabled = false;
    }

    public void Enter()
    {
        if (gameStarted)
            return;
        gameStarted = true;

        //标记格子并且创建边界
        FlagAndCreateBorder();

        //创建玩家
        CreatePlayer(RandomBornGrid());

    }
    public void Exit()
    {
    }

    // Update is called once per frame
    void Update()
    {

    }

    #region 创建对象、对象事件处理
    /// <summary>
    /// 绘制边界
    /// </summary>
    private void FlagAndCreateBorder()
    {
        Transform borderContainer = new GameObject("Border").transform;
        borderContainer.transform.SetParent(transform);

        Sprite bottomWall = Resources.Load<Sprite>("Textures/wallButtom");
        //绘制横向
        for (int x = 0; x < Width; x++)
        {
            grids.SetBorder(x, 0);
            GameObject top = CreateObject(m_BorderPref.name,m_BorderPref, x, 0);
            top.transform.parent = borderContainer;

            grids.SetBorder(x, Height - 1);
            GameObject bottom = CreateObject(m_BorderPref.name,m_BorderPref, x, Height - 1);
            SpriteRenderer br = bottom.GetComponent<SpriteRenderer>();
            br.sprite = bottomWall;
            bottom.transform.parent = borderContainer;
        }

        //绘制竖向
        for (int y = 0; y < Height; y++)
        {
            grids.SetBorder(0, y);
            GameObject left = CreateObject(m_BorderPref.name,m_BorderPref, 0, y);
            left.transform.parent = borderContainer;


            grids.SetBorder(Width - 1, y);
            GameObject right = CreateObject(m_BorderPref.name,m_BorderPref, Width - 1, y);
            right.transform.parent = borderContainer;
        }
        
    }

    public void ChangeTrackerName(string oldName, string newName)
    {
        Tracker tracker = GetTracker(oldName);
        tracker.ChangeName(newName);

        //Hash
        trackerHash.Remove(oldName);
        trackerHash.Add(newName, tracker);
    }

    /// <summary>
    /// 创建Tracker
    /// </summary>
    /// <returns></returns>
    public Tracker CreateTracker(GameObject prefab, Grid bornGrid, int job, string name, int textureID, bool isComputer)
    {
        //创建容器
        GameObject container = new GameObject(name);
        container.transform.SetParent(Map.ins.transform);

        //数据
        TrackerData data = new TrackerData();
        data.container = container.transform;
        data.name = name;
        data.job = job;
        data.isComputer = isComputer;
        data.textureID = textureID;
        data.order = TrackerOrder++;



        //创建Tracker
        GameObject go = CreateObject(prefab.name, prefab, bornGrid.x, bornGrid.y);
        Tracker tracker = go.GetComponent<Tracker>();
        tracker.Initialize(data);

        //AddEvent
        tracker.OnFillGrounds += CalcBottomShadow;
        tracker.OnBornStart += CalcBottomShadow;
        tracker.OnDeadEnd += CalcBottomShadow;

        //AddList
        trackerList.Add(tracker);
        trackerHash.Add(name, tracker);

        return tracker;
    }


    /// <summary>
    /// 创建玩家
    /// </summary>
    private void CreatePlayer(Grid born)
    {
		string name = "Test";

        m_Player = CreateTracker(m_TrackerPref, born, 1, name, 0, false);
        m_Player.gameObject.AddComponent<KeyboardController>();

        m_CameraFollow.m_Target = m_Player.transform;

        //侦听事件
        m_Player.OnDeadEnd += OnPlayerDeadEnd;
        m_Player.OnKillOther += OnPlayerKillOther;
        m_Player.OnContinuousKill += OnPlayerContinuousKill;

        humanPlayer = m_Player;

        //Born
        m_Player.Born(born);
    }


    /// <summary>
    /// 当玩家死亡动画结束后
    /// </summary>
    /// <param name="source"></param>
    /// <param name="args"></param>
    private void OnPlayerDeadEnd(Tracker target)
    {
        //  Tracker target = source as Tracker;

         Grid newBornPos = RandomBornGrid();
         target.Born(newBornPos);
    }

    private void OnPlayerKillOther(bool firstBlood)
    {
    }
    private void OnPlayerContinuousKill()
    {
    }

    public void PlayerReborn()
    {
        Grid newBornPos = RandomBornGrid();
        m_Player.Born(newBornPos);
    }
    /// <summary>
    /// 当电脑死亡动画结束后
    /// </summary>
    /// <param name="source"></param>
    /// <param name="args"></param>
    private void OnComputerDeadEnd(Tracker target)
    {
        //暂时把它移到“看不见的地方”
        target.transform.position = HidePosition;
   //     target.GetComponent<BornController>().CPUDelayBorn();

    }
    #endregion

    #region 随机出生
    private int[,] rectangleTestMap;

    /// <summary>
    /// 如果地图上不存在空白（6x6），则不复活
    /// </summary>
    /// <returns>true 可以复活 false 则不可</returns>
    public bool CheckComputerBorn()
    {
        List<Rectangle> rects = RectUtils.Split(CheckGirdIsBlock, bornRect);
        int w = 6;
        int h = 6;
        for (int i = 0, l = rects.Count; i < l; i++)
        {
            if (rects[i].width >= w && rects[i].height >= h)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 随机出生
    /// </summary>
    /// <returns></returns>
    public Grid RandomBornGrid()
    {
        List<Rectangle> rects = RectUtils.Split(CheckGirdIsBlock, bornRect);
        List<Rectangle> x9rects = GetNeedsRects(rects, 9, 9);// >= 9的格子 间距3格

        Rectangle targetRect;

        //左上角x y
        int x;
        int y;
        if (x9rects.Count > 0)
        {
            int idx = UnityEngine.Random.Range(0, x9rects.Count);
            targetRect = x9rects[idx];
            //距离边框3格，能保证离敌人3格
            x = RandomX(targetRect.x, targetRect.right, 3, 3 + Tracker.BornGroundSize[0]);
            y = RandomY(targetRect.y, targetRect.bottom, 3, 3 + Tracker.BornGroundSize[1]);
            return grids[x, y];
        }

        List<Rectangle> x3rects = GetNeedsRects(rects, 3, 3);

        if (x3rects.Count > 0)
        {
            int idx = UnityEngine.Random.Range(0, x3rects.Count);
            targetRect = x3rects[idx];
            x = targetRect.x + UnityEngine.Random.Range(0, targetRect.width - Tracker.BornGroundSize[1]);
            y = targetRect.y + UnityEngine.Random.Range(0, targetRect.height - Tracker.BornGroundSize[1]);
            return grids[x, y];
        }

        //没有“放置点”，随机一个点，Warrning：考虑随机到敌人“身上”的情况
        targetRect = Map.rect;
        x = RandomX(targetRect.x, targetRect.right, 3, 3 + Tracker.BornGroundSize[0]);
        y = RandomY(targetRect.y, targetRect.bottom, 3, 3 + Tracker.BornGroundSize[1]);
        return grids[x, y];
    }

    private bool CheckGirdIsBlock(int x, int y)
    {
        return !grids[x,y].IsNone;
    }

    public int RandomX(int left,int right,int l, int r)
    {
        return UnityEngine.Random.Range(left + l - 1, right - r + 1);
    }
    /// <summary>
    /// 随机Y
    /// </summary>
    /// <param name="t"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public int RandomY(int top,int bottom,int t, int b)
    {
        return UnityEngine.Random.Range(top + t - 1, bottom - b + 1);
    }



    /// <summary>
    /// 获取想要尺寸的矩形
    /// </summary>
    /// <returns></returns>
    private List<Rectangle> GetNeedsRects(List<Rectangle> rects,int width,int height)
    {
        List<Rectangle> needs = new List<Rectangle>();
        for (int i = 0, l = rects.Count; i < l; i++)
        {
            if (rects[i].width >= width && rects[i].height >= height)
            {
                needs.Add(rects[i]);
            }
        }
        return needs;
    }


    #endregion


    /// <summary>
    /// Rainfix:获取某格的下一格
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="grid"></param>
    /// <returns></returns>
    public static Grid getNextGrid(int direction,Grid grid)
    {
        int[,] array = new int[4, 2] { { 0, -1 }, { 1, 0 }, { 0, 1 }, { -1, 0 } };
		Grid nextCell = Map.grids.getGrid(grid.x + array[direction, 0], grid.y + array[direction, 1]);

        return nextCell;
    }

    private void OnJoystickMove(JoystickData data)
    {
        float angle360 = data.angle360;
        if (m_Player.isDead)
            return;
        if (angle360 >= 45 && angle360 < 135)
        {
            m_Player.SetChangeDirection(Direction.Up);
        }
        else if (angle360 >= 135 && angle360 < 225)
        {
            m_Player.SetChangeDirection(Direction.Left);
        }
        else if (angle360 >= 225 && angle360 < 315)
        {
            m_Player.SetChangeDirection(Direction.Down);
        }
        else
        {
            m_Player.SetChangeDirection(Direction.Right);
        }

        // print(angle360);
    }



    public void CalcBottomShadow(Tracker tracker)
    {
        CalcBottomShadow(tracker.topLeft[0], tracker.topLeft[1], tracker.bottomRight[0], tracker.bottomRight[1]);
    }
    /// <summary>
    /// 计算底部阴影
    /// 1、当Tracker填充时
    /// 2、当Tracker死亡时
    /// 3、当Tracker出生时
    /// </summary>
    private void CalcBottomShadow(int topLeftX,int topLeftY,int bottomRightX,int bottomRightY)
    {
        for (int y = topLeftY - 1, bottom = bottomRightY + 1; y <= bottom; y++)
        {
            for (int x = topLeftX; x <= bottomRightX; x++)
            {
                Grid current = grids[x, y];
                if (current.HasGround)
                {
                    Tracker groundOwner = GetTracker(current.groundOwner);
                    if (groundOwner == null)
                    {
                        Debug.LogError("找不到：" + current.groundOwner);
                        return;
                    }
                    Grid next = grids[x, y + 1];//下一个
                    bool isShadow = !next.HasGround;
                    if (isShadow)//检测是否需要设为阴影
                    {
                        grounds[x, y].sprite = groundOwner.m_GroundBottomSprite;
                    }
                    else
                    {
                        grounds[x, y].sprite = groundOwner.m_GroundSprite;
                    }
                }
            }
        }
    }


}
