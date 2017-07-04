using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

/*
internal class CornerGG
{
    public GameObject gameObject;
    public int inside;//内角方向
    public Grid grid;
}
 */
using UnityEngine.UI;

public delegate void ArrivedHandler(Tracker source,Grid arrived);

public class Tracker : MonoBehaviour {

#region 轨迹、拐角所用常量
    public static Vector2 GridSize = new Vector2(80f,80f);//一格大小
    public static Vector2 GridSizeMeter = GridSize * 0.01f;
    public static Vector2 TrackSize = new Vector2(80f, 80f);
    public static Vector2 TopLeftOffset = new Vector2(-Tracker.GridSize.x * 0.5f * 0.01f, Tracker.GridSize.y * 0.5f * 0.01f);

    private static Vector3 TrackHorizontal = new Vector3(0, GridSize.y / TrackSize.x, 0);
    private static Vector3 TrackVertical = new Vector3(0, GridSize.x / TrackSize.x, 0);

    private static Quaternion Rotation0 = Quaternion.Euler(Vector3.zero);
    private static Quaternion Rotation90 = Quaternion.Euler(new Vector3(0, 0, 90));
    private static Quaternion Rotation180 = Quaternion.Euler(new Vector3(0, 0, 180));
    private static Quaternion Rotation270 = Quaternion.Euler(new Vector3(0, 0, 270));

    private static Vector3 TrackRightPosition = new Vector3(-GridSize.x / 2 * 0.01f, 0, 0);
    private static Vector3 TrackUpPosition = new Vector3(0, -GridSize.y / 2 * 0.01f, 0);
    private static Vector3 TrackLeftPosition = new Vector3(GridSize.x / 2 * 0.01f, 0, 0);
    private static Vector3 TrackDownPosition = new Vector3(0, GridSize.y / 2 * 0.01f, 0);

    public static int[] BornGroundSize = new int[2] { 3, 3 };

    private static int[][] Direction4 = new int[4][]
    {
        new int[2]{-1,0},
        new int[2]{0,-1},
        new int[2]{1,0},
        new int[2]{0,1}
    };//左上右下

    private static int[][] Direction8 = new int[8][]
    {
        new int[2]{-1,-1},
        new int[2]{0,-1},
        new int[2]{1,-1},
        new int[2]{-1,0},
        new int[2]{1,0},
        new int[2]{-1,1},
        new int[2]{0,1},
        new int[2]{1,1}
    };//八方向

    public static float Speed = 4.5f;
    public static float OneGridTime = GridSizeMeter.x / Speed;//英雄正常速度走一格的时间(秒)
#endregion

    //Unity字段
    public GameObject m_TrackPref;
    public GameObject m_TrackCornerPref;
    public GameObject m_GroundPref;


    private Sprite m_TrackerSprite;
    private Sprite m_TrackSprite;
    [HideInInspector]
    public Sprite m_GroundSprite;
    public Sprite m_GroundBottomSprite;
    private Sprite m_CornerSprite;

    private SpriteRenderer m_TrackerRenderer;

    //事件
    public event EventHandler OnDeadStart;//死亡开始事件
    public event Action<Tracker> OnDeadEnd;//死亡结束
    public event ArrivedHandler OnArrived;//当达到一个格子

    public event Action<Tracker> OnFillGrounds;//当填充地盘完成（圈地）
    public event Action<bool> OnKillOther;//击杀其他时 是否是firstBlood
    public event Action OnContinuousKill;//当连杀时（> 1 才会派发）

    public event Action<Tracker> OnBornStart;//当出生（缓动开始）
    public event Action<Tracker> OnBornEnd;//缓动结束

    public bool isDead = false;//是否死亡
    public bool stopMove = false;
    public bool isBornTweening = false;//出生渐变中

   // private int job = 0;//职业 1 麦爹 2 大圣 3 Doge 4 死侍
  //  private string playerName = "Rick";
    private Grid bornGrid;//出生

    public float cdTotal = 20f;
    public float cdTime = 0f;//当前技能冷却时间
    public bool canChangeDir = true;//改变方向
    
    //特性：反跑
    public bool superBack = false;//是否可反向
    private bool backStart = false;//是否开始反向

    //特性：死亡后再原地盘复活（不算死亡）
    public bool superRevive = false;
    private int superReviveCount = 0;

    public Direction lastDirection = Direction.None;//上一个方向
    public Direction currentDirection = Direction.Up;
    public Direction changeDirection = Direction.None;//改变的方向

    /// <summary>
    /// 下一步移动的方向
    /// </summary>
    private Direction NextDirection
    {
        get{ return changeDirection != Direction.None ? changeDirection:currentDirection; }
    }

    /// <summary>
    /// 下一步到达的格子
    /// </summary>
    private Grid GetNextGrid(Grid grid)
    {
        int[] offset = Direction4[D2I(NextDirection)];
        return grids[grid.x + offset[0], grid.y + offset[1]];
    }


    private float gridMoveDistance = 0;//当前与上一个格子的距离

    private Grid lastGrid;//上一个Tracker的格子
    public Grid currentGrid;//当前Tracker所在的格子

    public int[] topLeft;//Ground + Track 左上角
    public int[] bottomRight;//Ground + Track 右下角
    public int[] groundTopleft;//仅在Arrvied时候同步，保证只记录groundTopleft
    public int[] groundBottomRight;

    private bool trackStart = false;//是否开始画“轨迹”
    private Grid trackStartFrom;//开始的格子（Ground）
    public List<Grid> allTrackGrids;//轨迹的所有格子（包括横向、竖向和拐角）

    public List<TrackSegment> allTrackSegment;//所有轨迹（线段）
    private List<GameObject> allTrackCornerGO;//所有拐角

    private TrackSegment currentSegment;//当前线段

    public MonoBehaviour skillActor;//身上的SkillActor

    public int deadGroundCount = 0;//死亡时地盘数量


    public const float ContinuousLimit = 10f;//连杀时间限制（秒）

    public uint killCount = 0;//一共击杀了多少人
    public uint continuousKill = 0;//连续击杀（超时会被清空）
	public uint bestContinuousKill=0;
    public float continuousTimer = 0;//连杀计时器

    private Transform container;//在Map下面的容器
    private GameObject shadow;//落下时的阴影
    public TrackerData data;

    public float speed = 1.0f;
    // Use this for initialization
    void Awake()
    {
        topLeft = new int[2];
        bottomRight = new int[2];
        groundTopleft = new int[2];
        groundBottomRight = new int[2];
        allTrackGrids = new List<Grid>();

        //allTrail = new List<Trail>();
        allTrackSegment = new List<TrackSegment>();
        allTrackCornerGO = new List<GameObject>();

        speed = Speed;
    }


    /// <summary>
    /// Awake(）之后带参初始化
    /// </summary>
    public void Initialize(TrackerData data)
    {
        this.data = data;
        this.container = data.container;

        transform.SetParent(container);

        //创建阴影
        GameObject shadowPref = Resources.Load<GameObject>("Prefabs/Shadow");
        shadow = GameObject.Instantiate(shadowPref);
        shadow.transform.SetParent(container);
        shadow.SetActive(false);

        //设置贴图（玩家通过Job，电脑通过textureID）
        if (data.isComputer)
        {
            ResetSprites();
        }
        else
        {
            this.Job = data.job;
        }
    }

    void Start()
    {
    }

    private void SetCurrentGrid(Grid g)
    {
        //设置上一个信息
        if (currentGrid != null)
        {
            lastGrid = currentGrid;
            lastGrid.trackerOn = "";
        }


        //设置我在此格子上
        if (g != null)
        {
            currentGrid = g;
            currentGrid.trackerOn = name;

            //记录当前位置
            if (currentGrid.x < topLeft[0])
                topLeft[0] = currentGrid.x;
            else if (currentGrid.x > bottomRight[0])
                bottomRight[0] = currentGrid.x;

            if (currentGrid.y < topLeft[1])
                topLeft[1] = currentGrid.y;
            else if (currentGrid.y > bottomRight[1])
                bottomRight[1] = currentGrid.y;
        }
        else
        {
            currentGrid = null;
        }
    }

    public string name
    {
        get { return data.name; }
    }

    public Transform Container
    {
        get { return transform.parent; }
    }

    public Grid CurrentGrid
    {
        get { return currentGrid; }
    }

    #region 出生、死亡


    /// <summary>
    /// 出生
    /// </summary>
    /// <param name="grid"></param>
    public void Born(Grid grid)
    {
        isDead = false;
        stopMove = false;
        canChangeDir = true;
        superBack = Job == 3;
        superRevive = Job == 4;
        superReviveCount = 0;
        backStart = false;
        cdTime = 0f;


        killCount = 0;
        continuousKill = 0;
        continuousTimer = 0f;

        this.bornGrid = grid;

        //Ground
        int topleftX = grid.x;
        int topleftY = grid.y;

        for (int y = topleftY, bottom = topleftY + 2; y <= bottom; y++)
        {
            for (int x = topleftX, right = topleftX + 2; x <= right; x++)
            {
                Map.SetGround(x, y, this,true);
            }
        }

        //位置
        SetCurrentGrid(Map.grids[topleftX + 1, topleftY + 1]);
        transform.position = Map.grids.Grid2WorldPosition(currentGrid);

        gridMoveDistance = 0f;

        //设置矩形
        topLeft[0] = topleftX; 
        topLeft[1] = topleftY;
        bottomRight[0] = topleftX + 2; 
        bottomRight[1] = topleftY + 2;
        groundTopleft[0] = topLeft[0];
        groundTopleft[1] = topLeft[1];
        groundBottomRight[0] = bottomRight[0];
        groundBottomRight[1] = bottomRight[1];

        //恢复透明度
        m_TrackerRenderer.DOFade(1f, 0f);

        //重置数据
        trackStart = false;
        trackStartFrom = null;

		//RainFix：设置默认方向
        ResetAllDirection();

        //出生缓动
        BornTweenStart();

        if (OnBornStart != null)
            OnBornStart(this);
    }

    private void BornTweenStart()
    {
        //一秒后再移动(Tween)
        isBornTweening = true;

        Vector3 from = transform.position;
        from.y += 2 * GridSizeMeter.y;//1格距离
        Vector3 to = transform.position;

        transform.position = from;

//		SpriteRenderer r=this.transform.FindChild("Tracker").GetComponent<SpriteRenderer>();
//		Color c1=new Color(r.color.r,r.color.g,r.color.b,0.2f);
//		Color c2=new Color(r.color.r,r.color.g,r.color.b,1f);
//		r.color=c1;

		Sequence sequence = DOTween.Sequence();
		sequence.AppendInterval(0.1f);
//		sequence.Join(r.DOColor(c2,0.1f));
		sequence.Append(transform.DOMove(to, 0.3f).SetEase(Ease.Linear));
		sequence.Append(transform.DOScale(new Vector3(1.5f,0.75f),0.1f));
		sequence.Append(transform.DOScale(new Vector3(1f,1f),0.1f));
		sequence.OnComplete(BornTweenEnd);

        //显示阴影
        shadow.transform.position = Map.grids.Grid2WorldPosition(currentGrid);
        shadow.SetActive(true);
    }

    private void BornTweenEnd()
    {
        isBornTweening = false;

        //隐藏阴影
        shadow.SetActive(false);

        if (OnBornEnd != null)
            OnBornEnd(this);
    }

    public void DeadStart(string who)
    {
        if (OnDeadStart != null)
            OnDeadStart(this, EventArgs.Empty);

        //清空当前所占格子TrackerOn
        SetCurrentGrid(null);

        //清空技能
        if (skillActor != null) Destroy(skillActor);


        deadGroundCount = ownGrounds.Count;
        isDead = true;

        ClearTrack();

		//RainFix:赋予默认方向
        ResetAllDirection();


        m_TrackerRenderer.DOFade(0, 1f).OnComplete(DeadEnd);

      //  DeadEnd();
    }
    private void DeadEnd()
    {
        if (!superRevive || superReviveCount > 0)
        {
            ClearGrounds();

            if (OnDeadEnd != null)
                OnDeadEnd(this);
        }
        else
        {
            superReviveCount++;
            ReviveInGround();
        }

        
    }
    #endregion
    
	


    public void TestRandomDir()
    {
        int i = UnityEngine.Random.Range(1, 5);
        Direction d = (Direction)Enum.Parse(typeof(Direction), i.ToString());
        SetChangeDirection(d);
    }

   

    void Update()
    {
        //连杀倒计时
        if (continuousKill > 0)
        {
            continuousTimer += Time.deltaTime;
            if (continuousTimer > ContinuousLimit)//连杀超时
            {
                continuousKill = 0;
            }
        }


        if (isDead)
            return;

        if (cdTime > 0)
        {
            cdTime -= Time.deltaTime;
        }
    }
	// Update is called once per frame
	void FixedUpdate () {
      //  return;
       // if (name != "Rick") return;//test

        if (stopMove || isDead || isBornTweening)
            return;

//        if (name == "Computer_3") return;
		if(currentGrid==null)return;

		if((int)this.currentDirection <0)
		{
			return;
		}

        float moveDistance = speed * Time.fixedDeltaTime;
        Move(moveDistance);
    }


    /// <summary>
    /// 每帧移动
    /// </summary>
    public void Move(float moveDistance)
    {
        Vector3 newPos = transform.position;
        switch (currentDirection)
        {
            case Direction.Left:
                newPos += -Vector3.right * moveDistance;
                break;
            case Direction.Up:
                newPos += Vector3.up * moveDistance;
                break;
            case Direction.Right:
                newPos += Vector3.right * moveDistance;
                break;
            case Direction.Down:
                newPos += -Vector3.up * moveDistance;
                break;
        }

        transform.position = newPos;

        TrackMove();

        CheckGrid(moveDistance);

		//if(trackStart) 
    }
    #region 格子操作
    private void CheckGrid(float moveDistance)
    {
        gridMoveDistance += moveDistance;
        while(gridMoveDistance >= GridSizeMeter.x)
        {
            gridMoveDistance = gridMoveDistance - GridSizeMeter.x;
            ArriveGrid();
        }
    }

    private void ArriveGrid()
    {
        Grid arrived;//到达的格子
        //判断方向
        switch (currentDirection)
        {
            case Direction.Left:
                arrived = Map.grids[currentGrid.x - 1, currentGrid.y];
                break;
            case Direction.Up:
                arrived = Map.grids[currentGrid.x, currentGrid.y - 1];
                break;
            case Direction.Right:
                arrived = Map.grids[currentGrid.x + 1, currentGrid.y];
                break;
            case Direction.Down:
                arrived = Map.grids[currentGrid.x, currentGrid.y + 1];
                break;
            default:
                arrived = null;
                break;
        }



        bool iDead;
        //在自己地盘
        if (!trackStart)
        {
            if (!IsMyGround(arrived))
            {
                //标记开始划线
                trackStart = true;
                trackStartFrom = currentGrid;
            }

            //撞到别人的“头”
            if (HitOtherTrackerHandler(arrived,out iDead) && iDead)
            {
                return;
            }
            //别人跑到我的地盘 && 被我撞到轨迹
            if (arrived.HasTrack && arrived.trackOwner != name)
            {
                Tracker target = Map.GetTracker(arrived.trackOwner);
                KillOther(target);
            }

            //在我的地盘 && 撞他的头
        }





		//BUG:圈地和路径绘制放在一个段落中，而机器人必须在路径消失前选出方向，导致明明圈地完成并消失的路径仍然成为了机器人前进的阻碍（机器人会躲开路径，而这些路径不该存在）
        
        //圈地中（不能用else，因为start之后的第一个格子就需要标记）
        if(trackStart)
        {
            //倒退移动中，根据倒退的特性，即在自己的轨迹中移动所以不需要进行碰撞判断
            if (backStart)
            {
                if (IsMyTrack(arrived))
                {
                    //下一次方向 和 输出方向相同，情况：来回走
                    if (NextDirection == arrived.trackOutDir)
                    {
                        currentSegment.count--;

                        backStart = false;
                    }
                    //和输入方向相同，情况：可能是拐角接上
                    else if (NextDirection == arrived.trackInDir)
                    {
                        Direction inDir = arrived.trackInDir;
                        InverseFlagHandle(arrived);
                        SetArrivedTrackFlag(arrived, inDir, NextDirection);//重新标记（但不生成新的Track）

                        //接上之前的线段
                        int i = allTrackSegment.Count - 1;
                        currentSegment = allTrackSegment[i];

                        backStart = false;
                    }
                    //下一次方向相反 和 输入方向相反 情况：不断回退
                    else if (IsRevert(NextDirection, arrived.trackInDir))
                    {
                        //清空
                        InverseFlagHandle(arrived);

                    }
                    //产生拐角 情况：另一个方向拐角
                    else
                    {
                        Direction inDir = arrived.trackInDir;
                        InverseFlagHandle(arrived);//clear
                        FlagHandle(arrived, inDir, NextDirection);

                        backStart = false;
                    }
                }
                //我的地盘
                else if (IsMyGround(arrived))
                {
                    backStart = false;
                }
                else
                {
                    throw new System.Exception("backStart 意外情况！");
                }
            }
            else
            {
                //判断是否反向
                backStart = IsRevert(currentDirection, NextDirection);
                
                //是否是正常轨迹
                bool normalTrack = false;

                //碰撞判断
                //到达我的“地盘”
                if (IsMyGround(arrived))
                {
                    //记录边界
                    groundTopleft[0] = topLeft[0];
                    groundTopleft[1] = topLeft[1];
                    groundBottomRight[0] = bottomRight[0];
                    groundBottomRight[1] = bottomRight[1];

                    bool startMiss = !IsMyGround(trackStartFrom);
                    if (!startMiss)
                    {
                        TestAndFill();//测试“围住”
                    }

                    ClearTrack();//清空轨迹
                }
                //撞到我的“线”
                else if (IsMyTrack(arrived))
                {
                    this.DeadStart(name);//自己死亡
                    return;
                }
                //预判撞到便捷
                else if(GetNextGrid(arrived).isBorder)
                {
                    this.DeadStart(name);
                    return;
                }
                //撞到边界
                else if (arrived.IsBorder)
                {
                    this.DeadStart(name);//自己死亡
                    return;
                }
                //撞到别人的“头”
                else if (HitOtherTrackerHandler(arrived, out iDead) && iDead)
                {
                    return;
                }
                //撞到别人的“线”
                else if (arrived.HasTrack && arrived.trackOwner != name)
                {
                    Tracker target = Map.GetTracker(arrived.trackOwner);
                    if (target != null)
                    {
                        KillOther(target);//杀死别人
                    }
                    else
                    {
                        Debug.LogError("意外情况：撞到别人了，可是找不到对象：" + arrived.trackOwner);
                    }

                    normalTrack = true;
                }
                else
                {
                    normalTrack = true;
                }

                if (normalTrack)
                {
                    //计数
                    if (currentSegment != null)
                    {
                        currentSegment.count++;
                    }

                    //标记格子
                    if (!backStart)
                    {
                        FlagHandle(arrived, currentDirection, NextDirection);
                    }
                }
            }

        }

        //所有判断之后再设置到达
        SetCurrentGrid(arrived);
        transform.position = Map.grids.Grid2WorldPosition(currentGrid);


        //派发到达事件
        if(OnArrived != null)
            OnArrived(this, arrived);

        //转向
        if (changeDirection != Direction.None)
        {
            SetDirection(changeDirection);
        }
    }


    #region 到达检测并处理函数
    /// <summary>
    /// 撞到别人的“头”
    /// </summary>
    /// <param name="arrived"></param>
    /// <returns></returns>
    private bool HitOtherTrackerHandler(Grid arrived,out bool iDead)
    {
		//BUG:自己会撞到自己把自己撞死,RainFix:去掉撞自己的BUG
		if (arrived.trackerOn != "")// && arrived.trackerOn!=this.name)
        {
            Tracker target = Map.GetTracker(arrived.trackerOn);
            //如果在我的“地盘”，仅他死，
            if (arrived.HasGround && arrived.groundOwner == name)
            {
                KillOther(target);
                iDead = false;
            }
            //如果在他的“地盘”，仅我死
            else if (arrived.HasGround && arrived.groundOwner == arrived.trackerOn)
            {
                target.KillOther(this);
                iDead = true;
            }
            else
            {
                //同归于尽
                if (target != null) target.DeadStart(name);
                this.DeadStart(name);
                iDead = true;
            }
            return true;
        }
        iDead = false;
        return false;
    }

    /// <summary>
    /// 撞到自己“轨迹”处理（
    /// </summary>
    /// <param name="arrived"></param>
    /// <returns></returns>
    private bool HitSelfTrackHandler(Grid arrived)
    {
        if (arrived.trackOwner == name)
        {
            this.DeadStart(name);//死亡
            return true;
        }
        return false;
    }
    private bool HitOtherTrackHandler(Grid arrived)
    {
        if (arrived.HasTrack && arrived.trackOwner != name)
        {
            Tracker target = Map.GetTracker(arrived.trackOwner);
            if (target != null)
            {
                KillOther(target);
            }
            return true;
        }
        return false;
    }
    #endregion

    /// <summary>
    /// 标记处理（）
    /// </summary>
    private void FlagHandle(Grid arrived,Direction inDir,Direction outDir)
    {
        SetArrivedTrackFlag(arrived, inDir, outDir);//设置到底格子的Track标记

        if (arrived.TrackIsCorner)//如果是拐角
        {
            if (currentSegment != null)
            {
                AlignTrack();
                StopTrackDraw();
            }
            CreateCornerGO(arrived);
        }
        else
        {
            //如果没有画线（第一格）开始画线
            if (currentSegment == null)
            {
                StartTrackDraw(arrived);
            }
        }
    }
    /// <summary>
    /// 反标记
    /// </summary>
    private void InverseFlagHandle(Grid arrived)
    {
        if (arrived.TrackIsCorner)
        {
            RemoveTailCornerGO();//删除拐角
        }
        else
        {
            if (currentSegment == null)
            {
                //"接上"上一段track
                int i = allTrackSegment.Count - 1;
                currentSegment = allTrackSegment[i];
            }
            currentSegment.count--;

            if (currentSegment.count == 0)
            {
                RemoveCurrentSegment();
            }
        }
        //清空标记
        ClearArrivedTrackFlag(arrived);
    }

    private void SetArrivedTrackFlag(Grid arrived,Direction inDir,Direction outDir)
    {
        TrackFlag flag;
        if (inDir == outDir)
        {
            flag = Direction2Flag(outDir);
        }
        else
        {
            flag = TwoDir2Flag(inDir, outDir);
        }
        Map.grids.SetTrack(arrived.x, arrived.y, flag, name, inDir, outDir);
        allTrackGrids.Add(arrived);
    }

    private void ClearArrivedTrackFlag(Grid arrived)
    {
        Map.grids.ClearTrack(arrived.x, arrived.y);
        allTrackGrids.Remove(arrived);
    }

    private void AlignTrack()
    {
        //对齐路径长度
        if (currentSegment != null)
        {
            Vector3 v = currentSegment.go.transform.localScale;
            v.x = currentSegment.count;
            SetTrackScale(v);
        }
    }

    /// <summary>
    /// 开始Track绘制
    /// </summary>
    private void StartTrackDraw(Grid arrived)
    {
        //创建轨迹对象
        CreateSegment(arrived, currentDirection);
    }

    /// <summary>
    /// Track停止绘制
    /// </summary>
    private void StopTrackDraw()
    {
        currentSegment = null;
    }


    private bool IsMyGround(Grid g)
    {
        return g.groundOwner == name;
    }
    private bool IsMyTrack(Grid g)
    {
        return g.HasTrack && g.trackOwner == name;
    }
    private bool IsMyGroundOrTrack(Grid g)
    {
        return (IsMyGround(g) || IsMyTrack(g));
    }

    /// <summary>
    /// 是否可以填充
    /// </summary>
    /// <returns></returns>
    private bool IsCanFill(Grid g)
    {
        return !IsMyGround(g) && !IsMyTrack(g);
    }

    #endregion

    #region 方向
    public void SetChangeDirection(Direction value)
    {
        if (!canChangeDir || value == currentDirection)
            return;

        //相反方向
        if (IsHorizontal(value) && IsHorizontal(currentDirection) || IsVertical(value) && IsVertical(currentDirection))
        {
            if (!superBack)
                return;
        }

        changeDirection = value;
    }

    public void SetDirection(Direction value)
    {
        lastDirection = currentDirection;
        currentDirection = value;
		changeDirection = Direction.None;
    }

    private void ResetAllDirection()
    {
        lastDirection = Direction.None;
        currentDirection = Direction.Up;
        changeDirection = Direction.None;
    }

    private bool IsHorizontal(Direction d)
    {
        return d == Direction.Left || d == Direction.Right;
    }
    private bool IsVertical(Direction d)
    {
        return d == Direction.Up || d == Direction.Down;
    }

    #endregion

    #region 轨迹
    private void CreateSegment(Grid grid,Direction dir)
    {
        GameObject currentTrack = Map.CreateObject("Track", m_TrackPref);
        Vector3 gridWorldPosition = Map.grids.Grid2WorldPosition(grid);

        currentSegment = new TrackSegment(currentTrack, gridWorldPosition);
        currentTrack.transform.SetParent(Container);

        SpriteRenderer currentTrackRenderer = currentSegment.renderer;
        Material currentTrackMaterial = currentSegment.material;
        currentTrackRenderer.sprite = m_TrackSprite;
        

        switch (dir)
        {
            case Direction.Right:
                currentTrack.transform.position = gridWorldPosition + TrackRightPosition;//靠左
                currentTrack.transform.rotation = Rotation0;
                SetTrackScale(TrackHorizontal);
                break;
            case Direction.Up:
                currentTrack.transform.position = gridWorldPosition + TrackUpPosition;
                currentTrack.transform.rotation = Rotation90;
                SetTrackScale(TrackVertical);
                break;
            case Direction.Left:
                currentTrack.transform.position = gridWorldPosition + TrackLeftPosition;
                currentTrack.transform.rotation = Rotation180;
                SetTrackScale(TrackHorizontal);
                break;
            case Direction.Down:
                currentTrack.transform.position = gridWorldPosition + TrackDownPosition;
                currentTrack.transform.rotation = Rotation270;
                SetTrackScale(TrackVertical);
                break;
        }
        allTrackSegment.Add(currentSegment);
        //allTrail.Add(currentTrail);
    }
    private void RemoveCurrentSegment()
    {
        allTrackSegment.Remove(currentSegment);
        Map.RecycleObject("Track", currentSegment.go);
        currentSegment = null;
    }

    private void TrackMove()
    {
        if (currentSegment == null)
            return;

        float trackMoveDistance = (transform.position - currentSegment.startPosition).magnitude;

        float pixel = trackMoveDistance * 100;
        float widthScale = pixel / TrackSize.x;
        Vector3 scale = currentSegment.go.transform.localScale;
        scale.x = widthScale + 0.1f;//+0.1f 是为了防止“穿帮”
        SetTrackScale(scale);
    }

    private void SetTrackScale(Vector3 scale)
    {
        currentSegment.go.transform.localScale = scale;
        currentSegment.material.SetFloat("RepeatX", scale.x);
    }

    private void CreateCornerGO(Grid grid)
    {
        GameObject cornerGO = Map.CreateObject("TrackCorner",m_TrackCornerPref);
        cornerGO.transform.SetParent(Container);
        cornerGO.transform.position = grids.Grid2WorldPosition(grid);

        SpriteRenderer cornerRenderer = cornerGO.GetComponent<SpriteRenderer>();
        cornerRenderer.sprite = m_CornerSprite;

        switch (grid.trackFlag)
        {
            case TrackFlag.TrackCorner0:
                cornerGO.transform.rotation = Rotation0;
                break;
            case TrackFlag.TrackCorner1:
                cornerGO.transform.rotation = Rotation90;
                break;
            case TrackFlag.TrackCorner2:
                cornerGO.transform.rotation = Rotation180;
                break;
            case TrackFlag.TrackCorner3:
                cornerGO.transform.rotation = Rotation270;
                break;
        }
        allTrackCornerGO.Add(cornerGO);
    }

    private void RemoveTailCornerGO()
    {
        int i = allTrackCornerGO.Count - 1;
        GameObject cornerGO = allTrackCornerGO[i];
        Map.RecycleObject("TrackCorner", cornerGO);
        allTrackCornerGO.RemoveAt(i);
    }

    
    #endregion

    #region 通用
    

    private TrackFlag Direction2Flag(Direction d)
    {
        return IsHorizontal(d) ? TrackFlag.HorizontalTrack : TrackFlag.VerticalTrack;
    }

    private TrackFlag TwoDir2Flag(Direction currentDirection, Direction nextDirection)
    {
        if (currentDirection == Direction.Down && nextDirection == Direction.Right || currentDirection == Direction.Left && nextDirection == Direction.Up)
        {
            return TrackFlag.TrackCorner0;
        }
        else if (currentDirection == Direction.Down && nextDirection == Direction.Left || currentDirection == Direction.Right && nextDirection == Direction.Up)
        {
            return TrackFlag.TrackCorner1;
        }
        else if (currentDirection == Direction.Right && nextDirection == Direction.Down || currentDirection == Direction.Up && nextDirection == Direction.Left)
        {
            return TrackFlag.TrackCorner2;
        }
        else if (currentDirection == Direction.Left && nextDirection == Direction.Down || currentDirection == Direction.Up && nextDirection == Direction.Right)
        {
            return TrackFlag.TrackCorner3;
        }
        return TrackFlag.TrackCorner0;
    }

    private bool IsRevert(Direction a,Direction b)
    {
        bool result = a == Direction.Left && b == Direction.Right ||
                a == Direction.Right && b == Direction.Left ||
                a == Direction.Up && b == Direction.Down ||
                a == Direction.Down && b == Direction.Up;//下一个方向是否相反

        return result;
    }

    #endregion

     #region 填充
    private Grids grids
    {
        get { return Map.grids; }
    }
    private int[,] fillTestMap;//1 表示通过竖向检测
    private void TestAndFill()
    {
        fillTestMap = new int[grids.Height, grids.Width];

//        grids.DebugTraceMap();

        HorizontalTest();//横线检测

       // DebugTestMap();//

        VerticalTest();//竖线检测

       // DebugTestMap();//

        SeedTest();//种子注入

      //  DebugTestMap();//

        FillGrids();//填充

		if(this==Map.humanPlayer)
			AdjustCamera();

        //派发圈地完成事件
        if (OnFillGrounds != null)
            OnFillGrounds(this);
    }

	private void AdjustCamera()
	{
		float num = 0.01f;  //拉远系数
		float num2 = 9f;    //最小值
		float a = 12f;   //最大值
		float num3 = num2 + (float)this.ownGrounds.Count * num;   //当前占地面积
		num3 = Mathf.Min(a, num3);
		num3 = Mathf.Floor(num3);
		//Todo：改为事件机制，解耦
		Camera.main.DOOrthoSize(num3, 0.5f);
	}

    #region 竖向检测

    private Grid FindGround(int x,int y,int dx, int dy)
    {
        x += dx;
        y += dy;
        for (; x >= topLeft[0] && y >= topLeft[1] && x <= bottomRight[0] && y <= bottomRight[1]; x+=dx, y+=dy)
        {
            if (IsMyGround(grids[x, y]))
            {
                return grids[x, y];
            }
        }

        Debug.LogError(string.Format("找不到我的地盘 x={0} y={1} dx={2} dy={3}", x, y, dx, dy));
        return null;
    }

    /// <summary>
    /// 是否是竖线连接点
    /// </summary>
    /// <param name="grid"></param>
    /// <returns></returns>
    private bool IsVerticalPoint(Grid grid)
    {
        if (grid.trackFlag == TrackFlag.HorizontalTrack
            || grid.trackFlag == TrackFlag.TrackCorner0
            || grid.trackFlag == TrackFlag.TrackCorner1)
            return true;

        if (grid.trackFlag == TrackFlag.TrackCorner2 && FindDownCornerOrGround(grid, TrackFlag.TrackCorner1))
            return true;

        if (grid.trackFlag == TrackFlag.TrackCorner3 && FindDownCornerOrGround(grid, TrackFlag.TrackCorner0))
            return true;

        return false;
    }
    /// <summary>
    /// 找到下方对应的拐角
    /// </summary>
    /// <param name="grid"></param>
    /// <returns></returns>
    private bool FindDownCornerOrGround(Grid grid,TrackFlag corner)
    {
        for (int y = grid.y + 1; y <= bottomRight[1]; y++)
        {
            Grid downGrid = grids[grid.x, y];
            if (downGrid.TrackIsCorner)
            {
                return downGrid.trackFlag == corner;
            }
            else if (IsMyGround(downGrid))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 竖向连接
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    private void VerticalLink(Grid a, Grid b)
    {
        //print("VerticalLink：" + a + "  " + b);
        for (int y = a.y + 1; y < b.y; y++)
        {
            Grid grid = grids[a.x, y];
            if (IsCanFill(grid))
            {
                fillTestMap[grid.x, grid.y] = 1;
            }
        }
    }


    /// <summary>
    /// 竖向检测
    /// </summary>
    private void VerticalTest()
    {
        for (int x = topLeft[0]; x <= bottomRight[0]; x++)
        {
            List<Grid> points = new List<Grid>();
            int groundIndex = -1;

            for (int y = topLeft[1]; y <= bottomRight[1]; y++)
            {
                Grid grid = grids[x, y];
                if (IsMyTrack(grid) && IsVerticalPoint(grid))
                {
                    points.Add(grid);
                }
                else if (groundIndex == -1 && IsMyGround(grid))
                {
                    groundIndex = points.Count;
                }
            }

            //奇数，加入Ground
            int count = points.Count;
            if (count % 2 != 0)
            {
                if (groundIndex == -1)
                {
                    continue;//“分割”的情况，只有一条“线”
                }

                Grid linkPoint;
                Grid linkGround;
                //奇数
                if (groundIndex % 2 != 0)
                {
                    linkPoint = points[groundIndex - 1];
                    linkGround = FindGround(linkPoint.x, linkPoint.y, 0, 1);
                }
                //偶数
                else
                {
                    linkPoint = points[groundIndex];
                    linkGround = FindGround(linkPoint.x, linkPoint.y, 0, -1);
                }

                points.Insert(groundIndex, linkGround);
            }

            for (int i = 0; i < count; i += 2)
            {
                VerticalLink(points[i], points[i + 1]);
            }
        }
        
    }
    #endregion

    #region 横向检测
    /// <summary>
    /// 查找右边的拐角
    /// </summary>
    /// <param name="grid"></param>
    /// <returns></returns>
    private bool FindRightCornerOrGround(Grid grid,TrackFlag corner)
    {
        for (int x = grid.x + 1; x <= bottomRight[0]; x++)
        {
            Grid rightGrid = grids[x, grid.y];
            if (rightGrid.TrackIsCorner)
            {
                return rightGrid.trackFlag == corner;
            }
            else if (IsMyGround(rightGrid))
                return true;
        }
        return false;
    }

    /// <summary>
    /// 是否是横线连接点
    /// </summary>
    /// <param name="grid"></param>
    /// <returns></returns>
    private bool IsHorizontalPoint(Grid grid)
    {
        if (grid.trackFlag == TrackFlag.VerticalTrack || grid.trackFlag == TrackFlag.TrackCorner1 || grid.trackFlag == TrackFlag.TrackCorner2)
            return true;

        if (grid.trackFlag == TrackFlag.TrackCorner3 && FindRightCornerOrGround(grid, TrackFlag.TrackCorner2))
            return true;

        if (grid.trackFlag == TrackFlag.TrackCorner0 && FindRightCornerOrGround(grid, TrackFlag.TrackCorner1))
            return true;

        return false;
    }


    private void HorizontalLink(Grid a, Grid b)
    {
        for (int x = a.x + 1; x < b.x; x++)
        {
            Grid grid = grids[x, a.y];
            if (IsCanFill(grid))
            {
                fillTestMap[grid.x, grid.y] = 1;
            }
        }
    }

    /// <summary>
    /// 横向测试，并输出“合格”的格子
    /// </summary>
    /// <returns></returns>
    private void HorizontalTest()
    {
        for (int y = topLeft[1]; y <= bottomRight[1]; y++)
        {
            List<Grid> points = new List<Grid>();//横向的点
            int groundIndex = -1;

            for (int x = topLeft[0]; x <= bottomRight[0];x++)
            {
                Grid grid = grids[x, y];
                if (IsMyTrack(grid) && IsHorizontalPoint(grid))
                {
                    points.Add(grid);
                }
                else if (groundIndex == -1 && IsMyGround(grid))
                {
                    groundIndex = points.Count;
                }
            }


            //奇数，加入Ground
            int count = points.Count;
            if (count % 2 != 0)
            {
                if (groundIndex == -1)
                {
                    continue;//“分割”的情况，只有一条“线”
                }

                Grid linkPoint;
                Grid linkGround;
                //奇数为终点，向右找
                if (groundIndex % 2 != 0)
                {
                    linkPoint = points[groundIndex - 1];
                    linkGround = FindGround(linkPoint.x, linkPoint.y, 1, 0);
                }
                //偶数为起点，向右找
                else
                {
                    linkPoint = points[groundIndex];
                    linkGround = FindGround(linkPoint.x, linkPoint.y, -1, 0);
                }
                points.Insert(groundIndex, linkGround);
            }

            for (int i = 0; i < count; i += 2)
            {
                HorizontalLink(points[i], points[i + 1]);
            }
        }
    }
    #endregion

    #region 种子注入
    
    //在种子填充时，需要检测是否“封闭”，将“封闭”的种子标记放入ClosedFlags里
    private const int SeedFlagStart = 2;
    private int CurrentSeedFlag;
    private List<int> SeedClosedFlags;

    public List<Grid> ownGrounds = new List<Grid>();//自己的所有“地盘”，由Map.SetGround动态更新
    public int OwnGroundCount
    {
        get { return ownGrounds.Count; }
    }


    private void SeedTest()
    {
        CurrentSeedFlag = SeedFlagStart;
        SeedClosedFlags = new List<int>();

        for (int y = topLeft[1] + 1; y < bottomRight[1]; y++)
        {
            for (int x = topLeft[0] + 1; x < bottomRight[0]; x++)
            {
                if (fillTestMap[x, y] == 1)
                {
                    if (SeedFill(grids[x, y]) == false)
                    {
                        CurrentSeedFlag++;
                        continue;
                    }

                    SeedClosedFlags.Add(CurrentSeedFlag++);
                }
            }
        }
    }

    private bool SeedFill(Grid g)
    {
        if (fillTestMap[g.x, g.y] == CurrentSeedFlag)
            return true;

        //到达边界
        if (g.x == topLeft[0] || g.x == bottomRight[0] || g.y == topLeft[1] || g.y == bottomRight[1])
            return false;

        fillTestMap[g.x, g.y] = CurrentSeedFlag;

        for (int i = 0; i < 4; i++)
        {
            int x = g.x + Direction4[i][0];
            int y = g.y + Direction4[i][1];
            if (IsCanFill(grids[x, y]))
            {
                if (SeedFill(grids[x, y]) == false)
                    return false;
            }
        }
        return true;
    }
    #endregion

    #region 填充
    private void FillGrids()
    {
        //轨迹格子 
        foreach (Grid g in allTrackGrids)
        {
            Map.SetGround(g.x, g.y, this,false);
        }

        //种子填充
        for (int y = topLeft[1] + 1; y < bottomRight[1]; y++)
        {
            for (int x = topLeft[0] + 1; x < bottomRight[0]; x++)
            {
                int flag = fillTestMap[x, y];
                if (flag >= SeedFlagStart && SeedClosedFlags.Contains(flag))
                {
                    Grid g = grids[x, y];
                    Map.SetGround(g.x, g.y, this,false);
                }
            }
        }
    }

    #endregion
    /// <summary>
    /// 清空轨迹（到达后）
    /// </summary>
    private void ClearTrack()
    {
        //Current
        currentSegment = null;
        trackStart = false;
        trackStartFrom = null;

        //GameObject
        foreach (TrackSegment segment in allTrackSegment)
        {
            Map.RecycleObject("Track", segment.go);
        }
        foreach (GameObject corner in allTrackCornerGO)
        {
            Map.RecycleObject("TrackCorner", corner);
        }

        allTrackSegment.Clear();
        allTrackCornerGO.Clear();

        //清理地图数据
        foreach (Grid g in allTrackGrids)
        {
            grids.ClearTrack(g.x, g.y);
        }

        //轨迹数据
        allTrackGrids.Clear();
    }

    /// <summary>
    /// 清空地面（由于地面是不固定的（会被人抢夺），所以Map的数据需要重新遍历一遍）
    /// </summary>
    private void ClearGrounds()
    {
        //清理地图数据
        for (int y = 0; y < grids.Height; y++)
        {
            for (int x = 0; x < grids.Width; x++)
            {
                if (IsMyGround(grids[x, y]))
                {
                    Map.ClearGround(x, y);
                }
            }
        }
    }
    #endregion

    #region 外围格子


    private List<Grid> GetAllOutsideGrounds()
    {
        List<Grid> oGrids = new List<Grid>();

        //横向
        for(int y = topLeft[1];y <= bottomRight[1];y++)
        {
            int left;
            for (left = topLeft[0]; left <= bottomRight[0]; left++)
            {
                if (IsMyGround(grids[left, y]))
                {
                    oGrids.Add(grids[left, y]);
                    break;
                }
            }
            for (int right = bottomRight[0]; right > left; right--)
            {
                if (IsMyGround(grids[right, y]))
                {
                    oGrids.Add(grids[right, y]);
                    break;
                }
            }
        }

        //纵向
        return null;
    }
    /// <summary>
    /// 获取一个最近的外围Ground
    /// </summary>
    /// <returns></returns>
    private Grid GetNearOutsideGround(Grid point)
    {
        Grid result = null;
        for (int i = 0, l = Mathf.Max(Map.Width, Map.Height); i < l; i++)
        {
            //四条直线点

            //
        }

        return result;
    }

    #endregion



    /// <summary>
    /// 职业 0普通英雄 1 麦爹 2 大圣 3 Doge 4 死侍
    /// </summary>
    public int Job
    {
        get { return data.job; }
        set { 
            data.job = value;
            

            if (!data.isComputer)
            {
                //加载贴图
                switch (data.job)
                {
                    case 1:
                        data.textureID = 0;
                        break;
                    case 2:
                        data.textureID = 2;
                        break;
                    case 3:
                        data.textureID = 1;
                        break;
                    case 4:
                        data.textureID = 3;
                        break;
                    default:
                        data.textureID = UnityEngine.Random.Range(10, 13);
                        break;
                }

                ResetSprites();
            }
        }
    }

    private void ResetSprites()
    {
        string id = data.textureID.ToString("00");
        string trackerTexName = "hero" + id;
        string trackTexName = "heroTail" + id;
        string groundTexName = "heroBase" + id;
        string cornerTexName = "heroTailCorner" + id;
        string bottomTexName = "heroBaseBottom" + id;

        m_TrackerSprite = Resources.Load<Sprite>("Textures/" + trackerTexName);
        m_TrackSprite = Resources.Load<Sprite>("Textures/" + trackTexName);
        m_GroundSprite = Resources.Load<Sprite>("Textures/" + groundTexName);
        m_CornerSprite = Resources.Load<Sprite>("Textures/" + cornerTexName);
        m_GroundBottomSprite = Resources.Load<Sprite>("Textures/" + bottomTexName);

		if(m_TrackerSprite==null)
			throw(new Exception("m_TrackerSprite==null"));
		if(m_GroundSprite==null)
			throw(new Exception("m_GroundSprite==null"));
		if(m_GroundBottomSprite==null)
			throw(new Exception("m_GroundBottomSprite==null"));
		
        m_TrackerRenderer = GetComponent<SpriteRenderer>();
        m_TrackerRenderer.sprite = m_TrackerSprite;
    }

    /// <summary>
    /// 是否在使用技能中
    /// </summary>
    public bool InSkill
    {
        get { return skillActor != null; }
    }

    
    public bool HasActiveSkill
    {
        get { return Job == 1 || Job == 2; }
    }

    #region 地盘复活
    private int[,] reviveTestMap;
    /// <summary>
    /// 设计思路：从“矩形”取一个中点，然后从这个中点八方向找地盘
    /// </summary>
    public void ReviveInGround()
    {
        //计算格子
        reviveTestMap = new int[grids.Height, grids.Width];

        int midX = groundTopleft[0] + (groundBottomRight[0] - groundTopleft[0]) / 2;
        int midY = groundTopleft[1] + (groundBottomRight[1] - groundTopleft[1]) / 2;
        Grid reviveGrid = null;
        if(IsMyGround(grids[midX,midY]))
        {
            reviveGrid = grids[midX,midY];
        }
        else
        {
            int max = Mathf.Max(midX,midY);
            int[] searchTL = new int[2];
            int[] searchBR = new int[2];
            for(int i = 1;i<=max;i++)
            {
                searchTL[0] = midX - i;
                searchTL[1] = midY - i;
                searchBR[0] = midX + i;
                searchBR[1] = midY + i;
                reviveGrid = OutsideFindMyGround(searchTL,searchBR);
                if (reviveGrid != null)
                    break;
            }
        }

        if (reviveGrid == null)
            throw new System.Exception("意外情况：ReviveInGround找不到格子！");

        //复活操作
        SetCurrentGrid(reviveGrid);
        transform.position = Map.grids.Grid2WorldPosition(currentGrid);

        gridMoveDistance = 0f;
        isDead = false;

        //恢复透明度
        m_TrackerRenderer.DOFade(1f, 0f);
        
    }

    /// <summary>
    /// 最外层搜索(左上开始)
    /// </summary>
    /// <param name="topLeft"></param>
    /// <param name="bottomRight"></param>
    /// <returns></returns>
    private Grid OutsideFindMyGround(int[] topLeft, int[] bottomRight)
    {
        //上
        for (int x = topLeft[0]; x <= bottomRight[0]; x++)
        {
            if (IsMyGround(grids[x, topLeft[1]]))
            {
                return grids[x, topLeft[1]];
            }
        }
        //右
        for (int y = topLeft[1]; y <= bottomRight[1]; y++)
        {
            if(IsMyGround(grids[bottomRight[0],y]))
            {
                return grids[bottomRight[0], y];
            }
        }

        //下
        for (int x = bottomRight[0]; x >= topLeft[0]; x--)
        {
            if (IsMyGround(grids[x, bottomRight[1]]))
            {
                return grids[x, bottomRight[1]];
            }
        }

        //左
        for (int y = bottomRight[1]; y >= topLeft[1]; y--)
        {
            if(IsMyGround(grids[topLeft[0],y]))
            {
                return grids[topLeft[0], y];
            }
        }

        return null;
    }
    #endregion

    public void ChangeName(string name)
    {
        data.name = name;
    }

    /// <summary>
    /// 杀死其他人
    /// </summary>
    public void KillOther(Tracker target)
    {
        killCount++;
        continuousKill++;
		if(bestContinuousKill<continuousKill)
			bestContinuousKill=continuousKill;
        continuousTimer = 0;

        target.DeadStart(this.name);

        if (OnKillOther != null)
            OnKillOther(killCount == 1);

        if (continuousKill > 1 && OnContinuousKill != null)
        {
            OnContinuousKill();
        }
    }

    public void DebugTestMap()
    {
        string output = "FillTestMap：\n";
        for (int y = 0; y < grids.Height; y++)
        {
            output += y + "行：";
            for (int x = 0; x < grids.Width; x++)
            {
                output += (int)fillTestMap[x, y] + ",";
            }
            output += "\n";
        }
        Debug.Log(output);
    }

    private int D2I(Direction dir)
    {
        switch (dir)
        {
            case Direction.Left:
                return 0;
            case Direction.Up:
                return 1;
            case Direction.Right:
                return 2;
            case Direction.Down:
                return 3;
        }
        return 0;
    }
}

public enum Direction
{
    None=-1,
    Left=3,
    Up=0,
    Right=1,
    Down=2
}


internal class Corner
{
    public CornerDirection direction;

    //线坐标
    public int x;
    public int y;

    public bool isHead;//第一个角
    public Corner prev;//输入角
    public Corner next;//输出角
    public int findDir;//查找方向 0 左 1 上 2 右 3 下
}

internal enum CornerSide
{
    Inside = 0,//内角
    Outside = 1,//外角
}

internal enum CornerDirection
{
    ToRightDown = 0,
    ToLeftDown,
    ToLeftUp,
    ToRightUp
}

public class Bottom
{
    public int y;
    public SpriteRenderer renderer;
    public Bottom(int y, SpriteRenderer renderer)
    {
        this.y = y;
        this.renderer = renderer;
    }
}

/// <summary>
/// 因为一条“线段”是多个Track格子 和 一个GameObject 组成的
/// </summary>
public class TrackSegment
{
    public GameObject go;
    public int count;
    public Vector3 startPosition;
    public SpriteRenderer renderer;
    public Material material;
    /*
    private GameObject currentTrack;//当前轨迹
    private Vector3 currentTrackStartGridPosition;
    private SpriteRenderer currentTrackRenderer;
    private Material currentTrackMaterial;//当前轨迹材质
    */
    public TrackSegment(GameObject go,Vector3 pos)
    {
        this.go = go;
        this.count = 0;
        this.startPosition = pos;
        this.renderer = go.GetComponent<SpriteRenderer>();
        this.material = renderer.material;
    }
}