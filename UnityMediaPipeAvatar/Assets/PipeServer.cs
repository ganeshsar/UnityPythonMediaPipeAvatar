using System.Collections;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;

/* Currently very messy because both the server code and hand-drawn code is all in the same file here.
 * But it is still fairly straightforward to use as a reference/base.
 */

[DefaultExecutionOrder(-1)]
public class PipeServer : MonoBehaviour
{
    public Transform bodyParent;
    public GameObject landmarkPrefab;
    public GameObject linePrefab;
    public GameObject headPrefab;
    public bool enableHead = false;
    public float multiplier = 10f;
    public float landmarkScale = 1f;
    public float maxSpeed = 50f;
    public float debug_samplespersecond;
    public int samplesForPose = 1;
    public bool active;

    private NamedPipeServerStream server;

    private Body body;

    // these virtual transforms are not actually provided by mediapipe pose, but are required for avatars.
    // so I just manually compute them
    private Transform virtualNeck;
    private Transform virtualHip;

    public Transform GetLandmark(Landmark mark)
    {
        return body.instances[(int)mark].transform ;
    }
    public Transform GetVirtualNeck()
    {
        return virtualNeck;
    }
    public Transform GetVirtualHip()
    {
        return virtualHip;
    }

    private void Start()
    {
        body = new Body(bodyParent,landmarkPrefab,linePrefab,landmarkScale,enableHead?headPrefab:null);
        virtualNeck = new GameObject("VirtualNeck").transform;
        virtualHip = new GameObject("VirtualHip").transform;

        Thread t = new Thread(new ThreadStart(Run));
        t.Start();

    }
    private void Update()
    {
        UpdateBody(body);
    }

    private void UpdateBody(Body b)
    {
        for (int i = 0; i < LANDMARK_COUNT; ++i)
        {
            if (b.positionsBuffer[i].accumulatedValuesCount < samplesForPose)
                continue;
            
            b.localPositionTargets[i] = b.positionsBuffer[i].value / (float)b.positionsBuffer[i].accumulatedValuesCount * multiplier;
            b.positionsBuffer[i] = new AccumulatedBuffer(Vector3.zero,0);
        }

        Vector3 offset = Vector3.zero;
        for (int i = 0; i < LANDMARK_COUNT; ++i)
        {
            Vector3 p = b.localPositionTargets[i]-offset;
            b.instances[i].transform.localPosition=Vector3.MoveTowards(b.instances[i].transform.localPosition, p, Time.deltaTime * maxSpeed);
        }

        virtualNeck.transform.position = (b.instances[(int)Landmark.RIGHT_SHOULDER].transform.position + b.instances[(int)Landmark.LEFT_SHOULDER].transform.position) / 2f;
        virtualHip.transform.position = (b.instances[(int)Landmark.RIGHT_HIP].transform.position + b.instances[(int)Landmark.LEFT_HIP].transform.position) / 2f;

        b.UpdateLines();
    }
    public void SetVisible(bool visible)
    {
        bodyParent.gameObject.SetActive(visible);
    }

    private void Run()
    {
        // Open the named pipe.
        server = new NamedPipeServerStream("UnityMediaPipeBody",PipeDirection.InOut, 99, PipeTransmissionMode.Message);

        print("Waiting for connection...");
        server.WaitForConnection();

        print("Connected.");
        var br = new BinaryReader(server);

        while (true)
        {
            try
            {
                Body h = body;
                var len = (int)br.ReadUInt32();
                var str = new string(br.ReadChars(len));

                string[] lines = str.Split('\n');
                foreach (string l in lines)
                {
                    if (string.IsNullOrWhiteSpace(l))
                        continue;
                    string[] s = l.Split('|');
                    if (s.Length < 4) continue;
                    int i;
                    if (!int.TryParse(s[0], out i)) continue;
                    h.positionsBuffer[i].value += new Vector3(float.Parse(s[1]), float.Parse(s[2]), float.Parse(s[3]));
                    h.positionsBuffer[i].accumulatedValuesCount += 1;
                    h.active = true;
                }
            }
            catch (EndOfStreamException)
            {
                break;                    // When client disconnects
            }
        }

    }

    private void OnDisable()
    {
        print("Client disconnected.");
        server.Close();
        server.Dispose();
    }

    const int LANDMARK_COUNT = 33;
    const int LINES_COUNT = 11;

    public struct AccumulatedBuffer
    {
        public Vector3 value;
        public int accumulatedValuesCount;
        public AccumulatedBuffer(Vector3 v, int ac)
        {
            value = v;
            accumulatedValuesCount = ac;
        }
    }

    public class Body
    {
        public Transform parent;
        public AccumulatedBuffer[] positionsBuffer = new AccumulatedBuffer[LANDMARK_COUNT];
        public Vector3[] localPositionTargets = new Vector3[LANDMARK_COUNT];
        public GameObject[] instances = new GameObject[LANDMARK_COUNT];
        public LineRenderer[] lines = new LineRenderer[LINES_COUNT];

        public bool active;

        public Body(Transform parent, GameObject landmarkPrefab, GameObject linePrefab, float s, GameObject headPrefab)
        {
            this.parent = parent;
            for (int i = 0; i < instances.Length; ++i)
            {
                instances[i] = Instantiate(landmarkPrefab);// GameObject.CreatePrimitive(PrimitiveType.Sphere);
                instances[i].transform.localScale = Vector3.one * s;
                instances[i].transform.parent = parent;
                instances[i].name = ((Landmark)i).ToString();
            }
            for (int i = 0; i < lines.Length; ++i)
            {
                lines[i] = Instantiate(linePrefab).GetComponent<LineRenderer>();
                lines[i].transform.parent = parent;
            }

            if (headPrefab)
            {
                GameObject head = Instantiate(headPrefab);
                head.transform.parent = instances[(int)Landmark.NOSE].transform;
                head.transform.localPosition = headPrefab.transform.position;
                head.transform.localRotation = headPrefab.transform.localRotation;
                head.transform.localScale = headPrefab.transform.localScale;
            }
        }
        public void UpdateLines()
        {
            lines[0].positionCount = 4;
            lines[0].SetPosition(0, Position((Landmark)32));
            lines[0].SetPosition(1, Position((Landmark)30));
            lines[0].SetPosition(2, Position((Landmark)28));
            lines[0].SetPosition(3, Position((Landmark)32));
            lines[1].positionCount = 4;
            lines[1].SetPosition(0, Position((Landmark)31));
            lines[1].SetPosition(1, Position((Landmark)29));
            lines[1].SetPosition(2, Position((Landmark)27));
            lines[1].SetPosition(3, Position((Landmark)31));

            lines[2].positionCount = 3;
            lines[2].SetPosition(0, Position((Landmark)28));
            lines[2].SetPosition(1, Position((Landmark)26));
            lines[2].SetPosition(2, Position((Landmark)24));
            lines[3].positionCount = 3;
            lines[3].SetPosition(0, Position((Landmark)27));
            lines[3].SetPosition(1, Position((Landmark)25));
            lines[3].SetPosition(2, Position((Landmark)23));

            lines[4].positionCount = 5;
            lines[4].SetPosition(0, Position((Landmark)24));
            lines[4].SetPosition(1, Position((Landmark)23));
            lines[4].SetPosition(2, Position((Landmark)11));
            lines[4].SetPosition(3, Position((Landmark)12));
            lines[4].SetPosition(4, Position((Landmark)24));

            lines[5].positionCount = 4;
            lines[5].SetPosition(0, Position((Landmark)12));
            lines[5].SetPosition(1, Position((Landmark)14));
            lines[5].SetPosition(2, Position((Landmark)16));
            lines[5].SetPosition(3, Position((Landmark)22));
            lines[6].positionCount = 4;
            lines[6].SetPosition(0, Position((Landmark)11));
            lines[6].SetPosition(1, Position((Landmark)13));
            lines[6].SetPosition(2, Position((Landmark)15));
            lines[6].SetPosition(3, Position((Landmark)21));

            lines[7].positionCount = 4;
            lines[7].SetPosition(0, Position((Landmark)16));
            lines[7].SetPosition(1, Position((Landmark)18));
            lines[7].SetPosition(2, Position((Landmark)20));
            lines[7].SetPosition(3, Position((Landmark)16));
            lines[8].positionCount = 4;
            lines[8].SetPosition(0, Position((Landmark)15));
            lines[8].SetPosition(1, Position((Landmark)17));
            lines[8].SetPosition(2, Position((Landmark)19));
            lines[8].SetPosition(3, Position((Landmark)15));

            lines[9].positionCount = 2;
            lines[9].SetPosition(0, Position((Landmark)10));
            lines[9].SetPosition(1, Position((Landmark)9));


            lines[10].positionCount = 5;
            lines[10].SetPosition(0, Position((Landmark)8));
            lines[10].SetPosition(1, Position((Landmark)5));
            lines[10].SetPosition(2, Position((Landmark)0));
            lines[10].SetPosition(3, Position((Landmark)2));
            lines[10].SetPosition(4, Position((Landmark)7));
        }

        public Vector3 Direction(Landmark from,Landmark to)
        {
            return (instances[(int)to].transform.position - instances[(int)from].transform.position).normalized;
        }
        public float Distance(Landmark from, Landmark to)
        {
            return (instances[(int)from].transform.position - instances[(int)to].transform.position).magnitude;
        }
        public Vector3 LocalPosition(Landmark Mark)
        {
            return instances[(int)Mark].transform.localPosition;
        }
        public Vector3 Position(Landmark Mark)
        {
            return instances[(int)Mark].transform.position;
        }

    }
}

public enum Landmark
{
    NOSE = 0,
    LEFT_EYE_INNER = 4,
    LEFT_EYE = 5,
    LEFT_EYE_OUTER = 6,
    RIGHT_EYE_INNER = 1,
    RIGHT_EYE = 2,
    RIGHT_EYE_OUTER = 3,
    LEFT_EAR = 8,
    RIGHT_EAR = 7,
    MOUTH_LEFT = 10,
    MOUTH_RIGHT = 9,
    LEFT_SHOULDER = 12,
    RIGHT_SHOULDER = 11,
    LEFT_ELBOW = 14,
    RIGHT_ELBOW = 13,
    LEFT_WRIST = 16,
    RIGHT_WRIST = 15,
    LEFT_PINKY = 18,
    RIGHT_PINKY = 17,
    LEFT_INDEX = 20,
    RIGHT_INDEX = 19,
    LEFT_THUMB = 22,
    RIGHT_THUMB = 21,
    LEFT_HIP = 24,
    RIGHT_HIP = 23,
    LEFT_KNEE = 26,
    RIGHT_KNEE = 25,
    LEFT_ANKLE = 28,
    RIGHT_ANKLE = 27,
    LEFT_HEEL = 30,
    RIGHT_HEEL = 29,
    LEFT_FOOT_INDEX = 32,
    RIGHT_FOOT_INDEX = 31,
    NONE = 40
}

