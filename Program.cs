using Skewboid;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// open csv file and store in an array of strings
string[] lines = File.ReadAllLines("..\\..\\..\\candidates.csv");
// convert the first line into an array of strings
string[] inputLine = lines[0].Split(',');
var index = Array.IndexOf(inputLine, "NumberOfObjectives");
if (index == -1) throw new Exception("NumberOfObjectives not found");
int numObjectives = int.Parse(inputLine[index + 1]);
index = Array.IndexOf(inputLine, "NumberOfCandidates");
if (index == -1) throw new Exception("NumberOfCandidates not found");
int numCandidates = int.Parse(inputLine[index + 1]);
index = Array.IndexOf(inputLine, "NumberOfAlphas");
if (index == -1) throw new Exception("NumberOfAlphas not found");
int numAlphas = int.Parse(inputLine[index + 1]);
index = Array.IndexOf(inputLine, "Weights");
var weights = Enumerable.Repeat(1.0, numObjectives).ToArray();
if (index != -1)
    for (int i = 0; i < numObjectives; i++)
    {
        if (double.TryParse(inputLine[index + i + 1], out var w))
            weights[i] = w;
    }

var candidates = new DefaultCandidate[numCandidates];
for (int i = 0; i < numCandidates; i++)
{
    string[] objString = lines[i + 1].Split(',');

    double[] objectives = new double[numObjectives];
    for (int j = 0; j < numObjectives; j++)
        if (double.TryParse(objString[j], out var f))
            objectives[j] = f;
    candidates[i] = new DefaultCandidate(objectives);
}

double[] alphaArray = new double[numAlphas];
for (int i = 0; i < numAlphas; i++)
{
    string[] objString = lines[i + 1+numCandidates].Split(',');
    if (double.TryParse(objString[0], out var f))
    {
        alphaArray[i] = f;
        List<ICandidate> paretoSet = ParetoFunctions.FindParetoCandidates(candidates, alphaArray[i], weights);
    }
}


