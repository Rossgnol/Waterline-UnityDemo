using System;
using System.Collections.Generic;
using UnityEngine;

namespace Waterline
{
    [Serializable]
    public sealed class ThreatNode
    {
        public string label;
        public Vector3 position;
        public int level;
        public int[] links;
    }

    public sealed class ThreatGraph : MonoBehaviour
    {
        public ThreatNode[] nodes;
        public int[] lowerPatrol;
        public int[] upperPatrol;
        public int upperArrival;

        public int Closest(Vector3 point, int level)
        {
            int nearest = -1; float distance = float.MaxValue;
            for (int i=0; i<nodes.Length; i++)
            {
                if (level != 2 && nodes[i].level != level) continue;
                float d = (nodes[i].position-point).sqrMagnitude;
                if (d < distance) { distance=d; nearest=i; }
            }
            return nearest;
        }

        public List<int> Path(int from, int to, int level)
        {
            var result=new List<int>();
            if (from<0 || to<0) return result;
            int count=nodes.Length;
            var cost=new float[count]; var previous=new int[count]; var visited=new bool[count];
            for(int i=0;i<count;i++) {cost[i]=float.MaxValue;previous[i]=-1;}
            cost[from]=0;
            for(int step=0;step<count;step++)
            {
                int current=-1; float best=float.MaxValue;
                for(int i=0;i<count;i++) if(!visited[i] && cost[i]<best){best=cost[i];current=i;}
                if(current<0 || current==to) break;
                visited[current]=true;
                foreach(int next in nodes[current].links)
                {
                    if(level!=2 && nodes[next].level!=level) continue;
                    float c=cost[current]+Vector3.Distance(nodes[current].position,nodes[next].position);
                    if(c<cost[next]){cost[next]=c;previous[next]=current;}
                }
            }
            if(cost[to]==float.MaxValue) return result;
            for(int i=to;i>=0;i=previous[i]) {result.Add(i);if(i==from)break;}
            result.Reverse(); return result;
        }

        private void OnDrawGizmosSelected()
        {
            if(nodes==null)return;
            Gizmos.color=Color.cyan;
            for(int i=0;i<nodes.Length;i++)
            {
                Gizmos.DrawSphere(nodes[i].position+Vector3.up*.1f,.12f);
                if(nodes[i].links==null)continue;
                foreach(int j in nodes[i].links) if(j>=0 && j<nodes.Length) Gizmos.DrawLine(nodes[i].position,nodes[j].position);
            }
        }
    }
}
