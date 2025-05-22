using DeNote.Models.Drawing;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeNote.Utils
{
    public class QIDrawingQuadTree<T> where T : QIDrawingObject
    {
        private const int MAX_OBJECTS = 10;
        private const int MAX_LEVELS = 5;

        private int level;
        private List<T> objects;
        private SKRect bounds;
        private QIDrawingQuadTree<T>[] nodes;

        public QIDrawingQuadTree(int level, SKRect bounds)
        {
            this.level = level;
            this.bounds = bounds;
            this.objects = new List<T>();
            this.nodes = new QIDrawingQuadTree<T>[4];
        }

        // 트리 초기화
        public void Clear()
        {
            objects.Clear();

            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i] != null)
                {
                    nodes[i].Clear();
                    nodes[i] = null;
                }
            }
        }

        // 하위 노드 분할
        private void Split()
        {
            float subWidth = bounds.Width / 2;
            float subHeight = bounds.Height / 2;
            float x = bounds.Left;
            float y = bounds.Top;

            // 4개의 사분면으로 분할
            nodes[0] = new QIDrawingQuadTree<T>(level + 1, new SKRect(x + subWidth, y, x + subWidth * 2, y + subHeight));
            nodes[1] = new QIDrawingQuadTree<T>(level + 1, new SKRect(x, y, x + subWidth, y + subHeight));
            nodes[2] = new QIDrawingQuadTree<T>(level + 1, new SKRect(x, y + subHeight, x + subWidth, y + subHeight * 2));
            nodes[3] = new QIDrawingQuadTree<T>(level + 1, new SKRect(x + subWidth, y + subHeight, x + subWidth * 2, y + subHeight * 2));
        }

        // 객체가 속할 인덱스 결정
        private int GetIndex(T obj)
        {
            int index = -1;
            SKRect objBounds = obj.Bounds;

            float verticalMidpoint = bounds.Left + (bounds.Width / 2);
            float horizontalMidpoint = bounds.Top + (bounds.Height / 2);

            // 상단 사분면에 속하는지
            bool topQuadrant = (objBounds.Top < horizontalMidpoint && objBounds.Bottom < horizontalMidpoint);
            // 하단 사분면에 속하는지
            bool bottomQuadrant = (objBounds.Top > horizontalMidpoint);

            // 왼쪽 사분면에 있는 객체
            if (objBounds.Left < verticalMidpoint && objBounds.Right < verticalMidpoint)
            {
                if (topQuadrant)
                    index = 1;
                else if (bottomQuadrant)
                    index = 2;
            }
            // 오른쪽 사분면에 있는 객체
            else if (objBounds.Left > verticalMidpoint)
            {
                if (topQuadrant)
                    index = 0;
                else if (bottomQuadrant)
                    index = 3;
            }

            return index;
        }

        // 객체 삽입
        public void Insert(T obj)
        {
            // 이미 하위 노드가 있으면 적절한 위치에 삽입
            if (nodes[0] != null)
            {
                int index = GetIndex(obj);

                if (index != -1)
                {
                    nodes[index].Insert(obj);
                    return;
                }
            }

            // 현재 노드에 객체 추가
            objects.Add(obj);

            // 최대 객체 수를 초과하고 최대 레벨에 도달하지 않았으면 분할
            if (objects.Count > MAX_OBJECTS && level < MAX_LEVELS)
            {
                // 하위 노드가 없으면 분할
                if (nodes[0] == null)
                {
                    Split();
                }

                // 기존 객체들을 적절한 하위 노드로 이동
                int i = 0;
                while (i < objects.Count)
                {
                    int index = GetIndex(objects[i]);
                    if (index != -1)
                    {
                        nodes[index].Insert(objects[i]);
                        objects.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                }
            }
        }

        // 객체 제거
        public void Remove(T obj)
        {
            // 현재 노드에서 객체 제거
            if (objects.Contains(obj))
            {
                objects.Remove(obj);
                return;
            }
            // 하위 노드가 있으면 하위 노드에서 제거
            if (nodes[0] != null)
            {
                for (int i = 0; i < nodes.Length; i++)
                {
                    nodes[i].Remove(obj);
                }
            }
        }

        // 특정 영역과 교차하는 객체 검색
        public List<T> Retrieve(List<T> returnObjects, SKRect area)
        {
            // 검색 영역이 현재 노드와 교차하는지 확인
            if (bounds.IntersectsWith(area))
            {
                // 현재 노드의 객체들 추가
                returnObjects.AddRange(objects);

                // 하위 노드가 있으면 검색 계속
                if (nodes[0] != null)
                {
                    for (int i = 0; i < nodes.Length; i++)
                    {
                        nodes[i].Retrieve(returnObjects, area);
                    }
                }
            }

            return returnObjects;
        }
    }
}
