using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Excel = Microsoft.Office.Interop.Excel;
using Office = Microsoft.Office.Core;
using Microsoft.Office.Tools.Excel;

namespace Skewboid
{
    public partial class SkewboidAddIn
    {
        List<ICandidate> candidates = new List<ICandidate>();

        private void ThisAddIn_Startup(object sender, System.EventArgs e)
        {


        }

        private void ThisAddIn_Shutdown(object sender, System.EventArgs e)
        {
        }

        #region VSTO generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InternalStartup()
        {
            this.Startup += new System.EventHandler(ThisAddIn_Startup);
            this.Shutdown += new System.EventHandler(ThisAddIn_Shutdown);
            this.Application.WorkbookOpen += new Excel.AppEvents_WorkbookOpenEventHandler(Application_WorkbookOpen);
        }
        #endregion

        void Application_WorkbookOpen(Excel.Workbook Wb)
        {
            candidates.Clear();
            Excel.Worksheet aWS = ((Excel.Worksheet)Application.ActiveSheet);

            int numObjectives = (int)aWS.Cells[2, 1].Value2;
            int numCandidates = (int)aWS.Cells[4, 1].Value2;
            int numMus = (int)aWS.Cells[6, 1].Value2;

            double[] weights = new double[numObjectives];
            try
            {
                for (int i = 0; i < numObjectives; i++)
                    weights[i] = aWS.Cells[9 + i, 1].Value2;
            }
            catch { weights = null; }

            for (int i = 0; i < numCandidates; i++)
            {
                double[] objectives = new double[numObjectives];
                for (int j = 0; j < numObjectives; j++)
                    objectives[j] = aWS.Cells[2 + i, 2 + j].Value2;
                candidates.Add(new candidate(objectives));
            }

            double[] mu = new double[numMus];
            for (int i = 0; i < numMus; i++)
            {
                var colIndex = 2 + numObjectives + i;
                mu[i] = aWS.Cells[1, colIndex].Value2;
                List<ICandidate> paretoSet = ParetoFunctions.FindParetoCandidates(candidates, mu[i], weights);

                for (int j = 0; j < numCandidates; j++)
                    aWS.Cells[2 + j, colIndex] = (paretoSet.Contains(candidates[j])) ? 1 : 0;
            }
        }
    }

}

