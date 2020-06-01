using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

[Serializable]
public class OSMelements {
    public double version;
    public string generator;
    public List<OSMObject> elements;
}

[Serializable]
public class OSMObject {
    public string type;
    public string name;
    public int id;
    public List<int> nodes;
    public List<OSMObject> members;
    public List<OSMObject> wayRefs;
    public List<OSMObject> nodeRefs;
    public Tags tags;
    public float lat;
    public float lon;
}

[Serializable]
public class Tags {
    public string name;
    public string addrcity;
    public string addrstreet;
    public string building;
    public string landuse;
}

public class Town {
    public Town (float x, float y, string _name) {
        x1 = x;
        x2 = x;
        y1 = y;
        y2 = y;
        name = _name;
    }
    public float x1;
    public float y1;
    public float x2;
    public float y2;
    public string name;
}

public class Retail {
    public Retail (OSMObject _w, float _x, float _y, GameObject _obj) {
        w = _w;
        city = w.tags.addrcity;
        street = w.tags.addrstreet;
        x = _x;
        y = _y;
        o = _obj;
    }
    public OSMObject w;
    public GameObject o;
    public string city;
    public string street;
    public float x;
    public float y;
}

public class FetchOSM : MonoBehaviour {
    public GameObject Building;
    public GameObject TownGO;
    float minlat = 41.784968F;
    float minlon = -88.016086F;
    float height = 0.056346F;
    float width = 0.156346F;
    /*float minlat = 40.058844F;
    float minlon = -88.340691F;*/
    float maxlat;
    float maxlon;
    float unitlat;
    float unitlon;
    float latToY (float lat) {
        return (lat - minlat) / unitlat;
    }
    float lonToX (float lon) {
        return (lon - minlon) / unitlon;
    }
    Town closestTown (float x, float y, List<Town> towns) {
        double distance = double.PositiveInfinity;
        Town closest = new Town (0, 0, "null");
        foreach (Town t in towns) {
            if (x < t.x2 && x > t.x1 && y < t.y2 && y > t.y1) {
                double dis = 0;
                dis += Math.Sqrt (Math.Pow (x - t.x1, 2) + Math.Pow (y - t.y1, 2));
                dis += Math.Sqrt (Math.Pow (x - t.x2, 2) + Math.Pow (y - t.y1, 2));
                dis += Math.Sqrt (Math.Pow (x - t.x1, 2) + Math.Pow (y - t.y2, 2));
                dis += Math.Sqrt (Math.Pow (x - t.x2, 2) + Math.Pow (y - t.y2, 2));
                if (dis < distance) {
                    distance = dis;
                    closest = t;
                }
            }

        }
        return closest;
    }

    List<Retail> removeOutliers (List<Retail> rs) {
        List<Retail> xordered = (from element in rs orderby element.x select element).ToList ();
        List<Retail> yordered = (from element in rs orderby element.y select element).ToList ();
        float xavg = xordered[xordered.Count / 2].x;
        float yavg = yordered[xordered.Count / 2].y;

        print (String.Format ("\t{0}, {1}", xavg, yavg));
        /*
        foreach (Retail r in rs) {
            xavg += r.x;
            yavg += r.y;
        }
        xavg /= rs.Count;
        yavg /= rs.Count;*/
        float variance = 0;
        foreach (Retail r in rs) {
            float distSq = (float) (Math.Pow (r.x - xavg, 2) + Math.Pow (r.y - yavg, 2));
            variance += distSq;
        }
        variance /= rs.Count;
        float stdv = (float) Math.Sqrt (variance);
        print (String.Format ("\tstdv {0}", stdv));
        List<Retail> clustered = new List<Retail> ();
        foreach (Retail r in rs) {
            float dist = (float) (Math.Pow (r.x - xavg, 2) + Math.Pow (r.y - yavg, 2));

            print (String.Format ("\t\tdist {0}; d-v {1}", dist, dist - variance));
            if (dist <= 2 * stdv) {
                clustered.Add (r);
            }
        }
        return clustered;
    }

    Vector2 averageVector (Vector2[] vecs) {
        float x = 0;
        float y = 0;
        foreach (Vector2 v in vecs) {
            x += v.x;
            y += v.y;
        }
        return new Vector2 (x / vecs.Length, y / vecs.Length);
    }
    // Start is called before the first frame update
    void Start () {
        Dictionary<int, OSMObject> elements;
        Dictionary<int, OSMObject> townElements;
        Dictionary<string, Town> towns = new Dictionary<string, Town> ();
        Dictionary<string, List<Retail>> retailAreas = new Dictionary<string, List<Retail>> ();
        List<OSMObject> objects;
        List<OSMObject> townObjs;
        //List<Building> houses;
        maxlat = minlat + height;
        maxlon = minlon + width;
        unitlat = (maxlat - minlat) / 32;
        unitlon = (maxlon - minlon) / (32 * (width/height));

        //WebRequest request = WebRequest.Create ("https://overpass-api.de/api/interpreter?data=[out:json];node(41.77,-87.95,41.82,-87.9);out;");
        //HttpWebResponse response = (HttpWebResponse) request.GetResponse ();
        using (WebClient wc = new WebClient ()) {
            //string query = String.Format ("https://overpass-api.de/api/interpreter?data=[out:json];node({0},{1},{2},{3});out;", minlat, minlon, maxlat, maxlon);
            string query = String.Format ("https://overpass-api.de/api/interpreter?data=[out:json];way[landuse=retail]({0},{1},{2},{3});(._;>;);out;", minlat, minlon, maxlat, maxlon);
            string townQuery = String.Format ("https://overpass-api.de/api/interpreter?data=[out:json][bbox:{0}, {1}, {2}, {3}];relation[admin_level=8];>>;out;", minlat, minlon, maxlat, maxlon);
            string json = wc.DownloadString (query);
            string townJson = wc.DownloadString (townQuery);
            townJson = townJson.Replace ("\"ref\"", "\"id\"");
            OSMelements data = JsonUtility.FromJson<OSMelements> (json);

            OSMelements townData = JsonUtility.FromJson<OSMelements> (townJson);
            print (query);
            objects = data.elements;
            townObjs = townData.elements;
            elements = data.elements.ToDictionary (x => x.id, x => x);
            townElements = townData.elements.ToDictionary (x => x.id, x => x);
        }
        int misses = 0;
        int queries = 0;
        List<OSMObject> toRemove = new List<OSMObject> ();
        //houses = new List<Building> ();
        foreach (OSMObject o in objects) {
            if (o.type != "way") {
                continue;
            }
            o.nodeRefs = new List<OSMObject> ();

            foreach (int i in o.nodes) {
                queries++;
                if (elements.ContainsKey (i)) {
                    o.nodeRefs.Add (elements[i]);
                } else {
                    misses++;
                    toRemove.Add (o);
                    break;
                }
            }
        }
        objects.RemoveAll (x => toRemove.Contains (x));
        toRemove = new List<OSMObject> ();
        foreach (OSMObject o in townObjs) {
            if (o.type == "relation") {
                o.wayRefs = new List<OSMObject> ();
                print (o.members.Count);
                print (o.members[0].id);
                foreach (OSMObject i in o.members) {
                    queries++;
                    if (townElements.ContainsKey (i.id)) {
                        o.wayRefs.Add (townElements[i.id]);
                    } else {
                        print (String.Format ("way {0} not here", i.id));
                        misses++;
                        toRemove.Add (o);
                        break;
                    }
                }
            } else if (o.type == "way") {
                o.nodeRefs = new List<OSMObject> ();

                foreach (int i in o.nodes) {
                    queries++;
                    if (townElements.ContainsKey (i)) {
                        o.nodeRefs.Add (townElements[i]);
                    } else {
                        print (String.Format ("node {0} not here", i));
                        misses++;
                        toRemove.Add (o);
                        break;
                    }
                }
            }

        }
        townObjs.RemoveAll (x => toRemove.Contains (x));
        foreach (OSMObject t in townObjs) {
            //print(t.type);
            if (t.type != "relation") {
                continue;
            }
            print ("RELATION");
            Town town;
            if (!towns.ContainsKey (t.tags.name)) {
                towns[t.tags.name] = new Town (-1, -1, t.tags.name);

            }
            town = towns[t.tags.name];
            foreach (OSMObject o in t.wayRefs) {
                if (o.type != "way") {
                    print (o.type);
                    continue;
                }
                print ("inner way has these many nodes:");
                print (o.nodeRefs.Count);
                print (o.nodes.Count);
                //print (String.Format ("instantiating at {0}, {1}", (o.nodeRefs[0].lat - minlat) / unitlat, (o.nodeRefs[0].lon - minlon) / unitlon));
                //Building h = new Building (o, latToY (o.nodeRefs[0].lat), lonToX (o.nodeRefs[0].lon));
                foreach (OSMObject n in o.nodeRefs) {
                    if (town.x1 == -1 || town.x1 > lonToX (n.lon)) {
                        town.x1 = lonToX (n.lon);
                        print (String.Format ("{0} -> {1}", n.lon, lonToX (n.lon)));
                    }
                    if (town.x2 == -1 || town.x2 < lonToX (n.lon)) {
                        town.x2 = lonToX (n.lon);
                        print (String.Format ("{0} -> {1}", n.lon, lonToX (n.lon)));
                    }
                    if (town.y1 == -1 || town.y1 > latToY (n.lat)) {
                        town.y1 = latToY (n.lat);
                        print (String.Format ("{0} -> {1}", n.lat, latToY (n.lat)));
                    }
                    if (town.y2 == -1 || town.y2 < latToY (n.lat)) {
                        town.y2 = latToY (n.lat);
                        print (String.Format ("{0} -> {1}", n.lat, latToY (n.lat)));
                    }
                }

            }
            print (String.Format ("town: {0}, x1={1}, x2={2}, y1={3}, y2={4}", town.name, town.x1, town.x2, town.y1, town.y2));
            var vertices2 = new Vector2[] {
                new Vector2 (town.x1, town.y1),
                new Vector2 (town.x2, town.y1),
                new Vector2 (town.x2, town.y2),
                new Vector2 (town.x1, town.y2)
            };
            /*
                        for (int i = 0; i < o.nodeRefs.Count - 1; i++) {
                            OSMObject n = o.nodeRefs[i];
                            vertices2[i] = new Vector2 (lonToX (n.lon), latToY (n.lat));
                            OSMObject ni = o.nodeRefs[i + 1];
                            Debug.DrawLine (new Vector3 (lonToX (n.lon), latToY (n.lat), 0), new Vector3 (lonToX (ni.lon), latToY (ni.lat)), w.tags.landuse == "commercial" || w.tags.landuse == "retail" ? Color.white :
                                w.tags.landuse == "grass" ? Color.green :
                                Color.cyan, 300, false);
                        }*/
            var vertices3 = System.Array.ConvertAll<Vector2, Vector3> (vertices2, v => new Vector3 (v.x, v.y, 12));
            var triangulator = new Triangulator (vertices2);
            var indices = triangulator.Triangulate ();
            var colors = Enumerable.Range (0, vertices3.Length)
                .Select (i => UnityEngine.Color.blue)
                .ToArray ();
            var mesh = new Mesh {
                vertices = vertices3,
                triangles = indices,
                colors = colors
            };
            mesh.RecalculateNormals ();
            mesh.RecalculateBounds ();
            var obj = Instantiate (TownGO, new Vector3 (0, 0, 0), Quaternion.identity);
            obj.name = town.name;
            var meshRenderer = obj.AddComponent<MeshRenderer> ();
            meshRenderer.material = new Material (Shader.Find ("Sprites/Default"));
            var filter = obj.AddComponent<MeshFilter> ();
            filter.mesh = mesh;
        }
        foreach (OSMObject o in objects) {
            if (o.type != "way") {
                continue;
            }
            var vertices2 = new Vector2[o.nodeRefs.Count - 1];

            for (int i = 0; i < o.nodeRefs.Count - 1; i++) {
                OSMObject n = o.nodeRefs[i];
                vertices2[i] = new Vector2 (lonToX (n.lon), latToY (n.lat));
                OSMObject ni = o.nodeRefs[i + 1];
                /*Debug.DrawLine (new Vector3 (lonToX (n.lon), latToY (n.lat), 0), new Vector3 (lonToX (ni.lon), latToY (ni.lat)), o.tags.landuse == "commercial" || o.tags.landuse == "retail" ? Color.white :
                    o.tags.landuse == "grass" ? Color.green :
                    Color.cyan, 300, false);*/
            }
            var vertices3 = System.Array.ConvertAll<Vector2, Vector3> (vertices2, v => new Vector3 (v.x, v.y, 0));
            var triangulator = new Triangulator (vertices2);
            var indices = triangulator.Triangulate ();
            var colors = Enumerable.Range (0, vertices3.Length)
                .Select (i => (o.tags.landuse == "retail" ? UnityEngine.Color.green : UnityEngine.Color.yellow))
                .ToArray ();
            var mesh = new Mesh {
                vertices = vertices3,
                triangles = indices,
                colors = colors
            };
            mesh.RecalculateNormals ();
            mesh.RecalculateBounds ();
            var obj = Instantiate (TownGO, new Vector3 (0, 0, 0), Quaternion.identity);
            Vector2 avg = averageVector (vertices2);
            Town t = closestTown (avg.x, avg.y, towns.Values.ToList ());
            Retail r = new Retail (o, avg.x, avg.y, obj);
            if (!retailAreas.ContainsKey (t.name)) {
                retailAreas[t.name] = new List<Retail> ();
            }
            retailAreas[t.name].Add (r);
            obj.name = String.Format ("{0}-{1}-{2}", t.name, avg.x, avg.y);
            var meshRenderer = obj.AddComponent<MeshRenderer> ();
            meshRenderer.material = new Material (Shader.Find ("Sprites/Default"));
            var filter = obj.AddComponent<MeshFilter> ();
            filter.mesh = mesh;
        }

        foreach (Town t in towns.Values.ToList ()) {
            if (!retailAreas.ContainsKey (t.name)) {
                continue;
            }

            print (String.Format ("{0} downtown:", t.name));
            List<Retail> downtown = removeOutliers (retailAreas[t.name]);
            print (String.Format ("\t{0} areas", downtown.Count));
            foreach (Retail r in downtown) {
                var filter = (MeshFilter) r.o.GetComponent ("MeshFilter");
                var mesh = filter.mesh;
                Color[] colors = new Color[mesh.vertices.Length];
                for (int m = 0; m < colors.Length; m++) {
                    colors[m] = Color.white;
                }
                mesh.colors = colors;
            }
        }
        print (String.Format ("{0} queries, {1} misses", queries, misses));
        print (String.Format ("{0} objects", objects.Count));
    }

    // Update is called once per frame
    void Update () {

    }
}