using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SecureSimulator;
using System.IO;

/*
ostarov@compute:~/bgp_sim2/bgp_sim$ mono TestingApplication/bin/Release/TestingApplication.exe Cyclops_caida_new.txt 174 32490 -q 32490 174 15169 32490 
Initialized and added 174
Initialized and added 32490
ASes from 32490 to 174, length: 3
32490
11071
174
26253
-
ASes from 15169 to 32490, length: 4
15169
174
11071
32490
26253
209
6461
-
*/
namespace TestingApplication
{
    class Program
    {
        static void Main(string[] args)
        {
		// Checking arguments
		if (args == null || args.Length < 3) {
		    Console.WriteLine("USAGE: mono TestingApplication.exe <input file> <dest1> ... <dstN> -q <src1> <dst1> ... <srcN> <dstN>");
		    return;
		}

		// Graph initialization
        	NetworkGraph g = new NetworkGraph();
		
		// E.g., input Cyclops_caida.txt
		if (File.Exists(args[0]))
                {
                    InputFileReader iFR = new InputFileReader(args[0], g);
                    iFR.ProcessFile();
                    Int32 p2pEdges = 0;
                    Int32 c2pEdges = 0;
                    foreach(var ASNode in g.GetAllNodes())
                    {
                        p2pEdges += ASNode.GetNeighborTypeCount(RelationshipType.PeerOf);
                        c2pEdges += ASNode.GetNeighborTypeCount(RelationshipType.CustomerOf);
                        c2pEdges += ASNode.GetNeighborTypeCount(RelationshipType.ProviderTo);
                    }
		    /*
                    Console.WriteLine("Read in the graph, it has " + g.NodeCount + " nodes and " + g.EdgeCount + " edges.");
                    Console.WriteLine("P2P: " + p2pEdges + " C2P: " + c2pEdges);
                    */
		}
                else
                {
                    Console.WriteLine("The file " + args[0] +  " does not exist.");
                    return;
                }
		
		// Setting destinations		
		List<Destination> d = new List<Destination>();
		Destination newD = new Destination();
		
		int i = 1;
		for (i = 1; i < args.Length; ++i) {
		    if ("-q" == args[i]) break;
		    if (initDestination(ref g, ref newD, args[i]))
		    {
		        d.Add(newD);
    		        
			Console.WriteLine("Initialized and added " + newD.destination);
    		        
		    }
		}
		
		// Approaching queries
		for (i = i+1; i < args.Length; i += 2) {
		    
		    int l = getPath(ref d, args[i], args[i+1]);
		    getAllPathsOfLength(ref d, l, args[i], args[i+1]);
		}	
		
		/*
		TestingClass test = new TestingClass();
		test.CLI(false);
		*/	 
        }

	private static bool initDestination(ref NetworkGraph g, ref Destination d, string dest)
        {
            UInt32 destNum;
            if (!UInt32.TryParse(dest, out destNum))
            {
		/*
                Console.WriteLine("Invalid ASN!");
                */
		return false;
            }
            if (g.GetNode(destNum) == null)
            {
		/*
                Console.WriteLine("WARNING: Could not retrieve destination " + d + " from the graph.");
                */
		return false;
            }

	    /*
            Console.WriteLine("Initializing variables and running RTA");
            */
	    MiniDestination miniDest = SimulatorLibrary.initMiniDestination(g, destNum, false);
            d = new Destination(miniDest);
            bool[] tempS = new bool[Constants._numASNs];
            for (int i = 0; i < tempS.Length; i++) {
                 tempS[i] = false;
            }
	    d.UpdatePaths(tempS);
            /*
	    Console.WriteLine("Done initializing. Current active destination is: " + destNum);
            */
	    return true;
        }

	private static int getPath(ref List<Destination> ds, string src, string dst)
        {
            int dstNum;
            UInt32 ASN;
            if (!UInt32.TryParse(src, out ASN) || !int.TryParse(dst, out dstNum))
            {
		/*
                Console.WriteLine("Invalid ASN or destination.");
                */
		return 0;
            }

            foreach (Destination d in ds)
            {
                if (d.destination == dstNum)
                {
                    //Console.WriteLine("> Path from " + ASN + " to " + d.destination + " is " + d.GetPath(ASN));
                    
		    string tmp = d.GetPath(ASN);

		    tmp = tmp.Replace("-", "");
		    tmp = tmp.Replace("<", "");
		    tmp = tmp.Replace(">", ""); 
		    tmp = tmp.Replace("  ", " ");

		    //Console.WriteLine(tmp);
		    string[] ases = tmp.Split(' ');
		    return ases.Length;
                }
            }
	    /*
            Console.WriteLine("WARNING: Could not find destination!");
            */
	    return 0;
        }

	private static bool getAllPathsOfLength(ref List<Destination> ds, int length, string src, string dst)
	{
		Console.WriteLine("ASes from " + src + " to " + dst + ", length: " + length);

		int dstNum;
        	UInt32 ASN;
            	if (!UInt32.TryParse(src, out ASN) || !int.TryParse(dst, out dstNum))
            	{
                	/*
                	Console.WriteLine("Invalid ASN or destination.");
                	*/
                	return false;
            	}

		//Console.WriteLine("ASes from " + ASN + " to " + dstNum);

		foreach (Destination d in ds)
		{
			if (d.destination == dstNum)
			{
				if (d.BestNew[ASN] != null)
				{
					HashSet<string> pathSet = new HashSet<string>(); 

					//d.Best[0]++;
					List<List<UInt32>> allPaths = new List<List<UInt32>>();
					List<UInt32> pathNew = new List<UInt32>();
					UInt32 first = (UInt32)((ASN << 3) + Destination._NOREL);
					pathNew.Add(first);
					//Console.Write("First: " + (UInt32)(((uint)first) >> 3) + "\n");
					
					TextWriter tw = null;	// Looks like not needed anymore
					int count = 0;
					d.GetAllPaths(ASN, (UInt32)dstNum, ref allPaths, pathNew, ref tw, ref count);

					int counter = 0;
					for (int j = 0; j < allPaths.Count; j++)
					{
						if (allPaths [j].Count == length) {
							pathToSet(ref pathSet, allPaths[j]);
							counter++;
						}
					}
					allPaths.Clear();

					
					//Console.WriteLine(counter + " Paths from " + ASN + " to " + dstNum);
					
					string[] arr = pathSet.ToArray();
					
					if (arr.Length > 0) {
						Console.WriteLine(string.Join("\n", arr));
						Console.WriteLine("-");
					}
					
					return true;
				}
				else
				{
					//Console.WriteLine("No path from " + ASN);
				}
			}
		}

		Console.WriteLine("-");

		//Console.WriteLine("could not find destination");
		return false;
	}

	private static void pathToSet(ref HashSet<string> res, List<UInt32> path)
        {
            for (int j = 0; j < path.Count; j++)
            {
                //int col = (int)(path[j] & 7);
     		res.Add(Convert.ToString((UInt32)(((uint)path[j]) >> 3)));
            }
        }

    }

}
