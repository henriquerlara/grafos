using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace GraphVisualizationConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Write("Digite o nome do arquivo contendo o grafo: ");
            string filename = Console.ReadLine();

            Console.Write("Digite o número do vértice para classificação das arestas divergentes: ");
            if (!int.TryParse(Console.ReadLine(), out int startVertex))
            {
                Console.WriteLine("Número de vértice inválido.");
                return;
            }

            Graph graph;
            try
            {
                graph = ReadGraphFromFile(filename);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao ler o arquivo: {ex.Message}");
                return;
            }

            graph.SortAdjacency();

            DFSResult dfsResult = new DFSResult(graph.NumVertices);
            bool[] visited = new bool[graph.NumVertices + 1];
            for (int u = 1; u <= graph.NumVertices; u++)
            {
                if (!visited[u])
                {
                    DFSIterative(u, graph, visited, dfsResult);
                }
            }

            Console.WriteLine("\nArestas de Árvore encontradas:");
            foreach (string edge in dfsResult.TreeEdges)
            {
                Console.WriteLine(edge);
            }

            Console.WriteLine($"\nClassificação das arestas divergentes do vértice {startVertex}:");
            ClassifyEdges(startVertex, graph, dfsResult);

            string dotFilename = "grafo.dot";
            try
            {
                GenerateDotFile(graph, dotFilename, dfsResult);
                Console.WriteLine($"\nArquivo DOT gerado: {dotFilename}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao gerar o arquivo DOT: {ex.Message}");
                return;
            }

            string imageFilename = "grafo.png";
            try
            {
                GenerateImageFromDot(dotFilename, imageFilename);
                Console.WriteLine($"\nImagem do grafo gerada: {imageFilename}");
                OpenImage(imageFilename);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao gerar a imagem: {ex.Message}");
            }

            Console.WriteLine("Processo concluído. Pressione qualquer tecla para sair.");
            Console.ReadKey();
        }

        class Graph
        {
            public int NumVertices { get; }
            public int NumEdges { get; }
            public List<List<int>> AdjacencyList { get; }

            public Graph(int n, int m)
            {
                NumVertices = n;
                NumEdges = m;
                AdjacencyList = new List<List<int>>();
                for (int i = 0; i <= n; i++)
                {
                    AdjacencyList.Add(new List<int>());
                }
            }

            public void AddEdge(int u, int v)
            {
                AdjacencyList[u].Add(v);
            }

            public void SortAdjacency()
            {
                for (int i = 1; i <= NumVertices; i++)
                {
                    AdjacencyList[i].Sort();
                }
            }
        }

        class DFSResult
        {
            public List<string> TreeEdges { get; }
            public int[] DiscoveryTime { get; }
            public int[] FinishTime { get; }
            public int[] Parent { get; }
            public int Time { get; set; }

            public DFSResult(int n)
            {
                TreeEdges = new List<string>();
                DiscoveryTime = new int[n + 1];
                FinishTime = new int[n + 1];
                Parent = new int[n + 1];
                for (int i = 0; i < Parent.Length; i++)
                {
                    Parent[i] = -1;
                }
                Time = 0;
            }
        }

        class StackFrame
        {
            public int Vertex { get; set; }
            public int NeighborIndex { get; set; }

            public StackFrame(int vertex)
            {
                Vertex = vertex;
                NeighborIndex = 0;
            }
        }

        static Graph ReadGraphFromFile(string filename)
        {
            if (!File.Exists(filename))
                throw new FileNotFoundException("Arquivo não encontrado.");

            string[] lines = File.ReadAllLines(filename);
            if (lines.Length < 1)
                throw new Exception("Arquivo vazio.");

            string[] firstLine = lines[0].Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (firstLine.Length < 2)
                throw new Exception("Formato inválido na primeira linha.");

            if (!int.TryParse(firstLine[0], out int n))
                throw new Exception("Número de vértices inválido.");

            if (!int.TryParse(firstLine[1], out int m))
                throw new Exception("Número de arestas inválido.");

            if (lines.Length < m + 1)
                throw new Exception("Número de arestas insuficiente no arquivo.");

            Graph graph = new Graph(n, m);

            for (int i = 1; i <= m; i++)
            {
                string[] parts = lines[i].Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2)
                    throw new Exception($"Formato inválido na aresta {i}.");

                if (!int.TryParse(parts[0], out int u))
                    throw new Exception($"Origem inválida na aresta {i}.");

                if (!int.TryParse(parts[1], out int v))
                    throw new Exception($"Destino inválido na aresta {i}.");

                graph.AddEdge(u, v);
            }

            return graph;
        }

        static void DFSIterative(int startVertex, Graph graph, bool[] visited, DFSResult result)
        {
            Stack<StackFrame> stack = new Stack<StackFrame>();
            stack.Push(new StackFrame(startVertex));

            result.Time++;
            result.DiscoveryTime[startVertex] = result.Time;
            visited[startVertex] = true;

            while (stack.Count > 0)
            {
                StackFrame frame = stack.Peek();
                int currentVertex = frame.Vertex;

                if (frame.NeighborIndex < graph.AdjacencyList[currentVertex].Count)
                {
                    int neighbor = graph.AdjacencyList[currentVertex][frame.NeighborIndex];
                    frame.NeighborIndex++;

                    if (!visited[neighbor])
                    {
                        result.Parent[neighbor] = currentVertex;
                        result.TreeEdges.Add($"{currentVertex} -> {neighbor}");
                        result.Time++;
                        result.DiscoveryTime[neighbor] = result.Time;
                        visited[neighbor] = true;

                        stack.Push(new StackFrame(neighbor));
                    }
                }
                else
                {
                    result.Time++;
                    result.FinishTime[currentVertex] = result.Time;
                    stack.Pop();
                }
            }
        }

        static void ClassifyEdges(int u, Graph graph, DFSResult dfsResult)
        {
            foreach (int v in graph.AdjacencyList[u])
            {
                if (dfsResult.Parent[v] == u)
                {
                    Console.WriteLine($"{u} -> {v} : Aresta de Árvore");
                }
                else if (dfsResult.DiscoveryTime[v] < dfsResult.DiscoveryTime[u] && dfsResult.FinishTime[v] > dfsResult.FinishTime[u])
                {
                    Console.WriteLine($"{u} -> {v} : Aresta de Retorno");
                }
                else if (dfsResult.DiscoveryTime[u] < dfsResult.DiscoveryTime[v] && dfsResult.FinishTime[u] > dfsResult.FinishTime[v])
                {
                    Console.WriteLine($"{u} -> {v} : Aresta de Avanço");
                }
                else
                {
                    Console.WriteLine($"{u} -> {v} : Aresta Cruzada");
                }
            }
        }

        static void GenerateDotFile(Graph graph, string dotFilename, DFSResult dfsResult)
        {
            using (StreamWriter sw = new StreamWriter(dotFilename))
            {
                sw.WriteLine("digraph G {");
                sw.WriteLine("    node [shape=circle, style=filled, color=lightblue];");

                for (int i = 1; i <= graph.NumVertices; i++)
                {
                    sw.WriteLine($"    {i};");
                }

                for (int u = 1; u <= graph.NumVertices; u++)
                {
                    foreach (int v in graph.AdjacencyList[u])
                    {
                        string color = "black";
                        if (dfsResult.TreeEdges.Contains($"{u} -> {v}"))
                        {
                            color = "red";
                        }
                        else if (dfsResult.DiscoveryTime[v] < dfsResult.DiscoveryTime[u] && dfsResult.FinishTime[v] > dfsResult.FinishTime[u])
                        {
                            color = "blue";
                        }
                        else if (dfsResult.DiscoveryTime[u] < dfsResult.DiscoveryTime[v] && dfsResult.FinishTime[u] > dfsResult.FinishTime[v])
                        {
                            color = "green";
                        }
                        else
                        {
                            color = "orange";
                        }

                        sw.WriteLine($"    {u} -> {v} [color={color}];");
                    }
                }

                sw.WriteLine("}");
            }
        }

        static void GenerateImageFromDot(string dotFilename, string imageFilename)
        {
            ProcessStartInfo checkGraphviz = new ProcessStartInfo
            {
                FileName = "dot",
                Arguments = "-V",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using (Process process = Process.Start(checkGraphviz))
                {
                    process.WaitForExit();
                    if (process.ExitCode != 0)
                    {
                        throw new Exception("Graphviz não está instalado ou não está no PATH.");
                    }
                }
            }
            catch (Exception)
            {
                throw new Exception("Graphviz não está instalado ou não está no PATH.");
            }

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "dot",
                Arguments = $"-Tpng {dotFilename} -o {imageFilename}",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(startInfo))
            {
                string stdout = process.StandardOutput.ReadToEnd();
                string stderr = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new Exception($"Graphviz retornou um erro: {stderr}");
                }
            }
        }

        static void OpenImage(string imagePath)
        {
            if (File.Exists(imagePath))
            {
                try
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = imagePath,
                        UseShellExecute = true
                    };
                    Process.Start(startInfo);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao abrir a imagem: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("Arquivo de imagem não encontrado.");
            }
        }
    }
}
