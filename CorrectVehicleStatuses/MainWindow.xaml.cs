using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NewEventLogDLL;
using VehicleProblemsDLL;
using VehiclesInShopDLL;

namespace CorrectVehicleStatuses
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //setting up the classes
        WPFMessagesClass TheMessagesClass = new WPFMessagesClass();
        EventLogClass TheEventLogClass = new EventLogClass();
        VehicleProblemClass TheVehicleProblemClass = new VehicleProblemClass();
        VehiclesInShopClass TheVehiclesInShopClass = new VehiclesInShopClass();

        //setting up the data
        FindAllVehicleProblemStatusesDataSet aFindAllVehicleProblemStatusesDataSet;
        FindAllVehicleProblemStatusesDataSet TheFindAllVehicleProblemStatusesDataSet;
        FindAllVehicleProblemStatusesDataSetTableAdapters.FindAllVehicleProblemsForStatusesTableAdapter aFindAllVehicleProblemStatusesTableAdapter;
        FindVehiclesInShopByVehicleIDDataSet TheFindVehiclesInShopByVehicleIDDataSet = new FindVehiclesInShopByVehicleIDDataSet();
        FindVehicleMainProblemUpdateByProblemIDDataSet TheFindVehicleMainProblemUpdateByProblemIDDataSet = new FindVehicleMainProblemUpdateByProblemIDDataSet();

        string gstrProblemResolution;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TheFindAllVehicleProblemStatusesDataSet = FindAllVehicleProblemStatuses();

            dgrResults.ItemsSource = TheFindAllVehicleProblemStatusesDataSet.FindAllVehicleProblemsForStatuses;
        }
        private FindAllVehicleProblemStatusesDataSet FindAllVehicleProblemStatuses()
        {
            try
            {
                aFindAllVehicleProblemStatusesDataSet = new FindAllVehicleProblemStatusesDataSet();
                aFindAllVehicleProblemStatusesTableAdapter = new FindAllVehicleProblemStatusesDataSetTableAdapters.FindAllVehicleProblemsForStatusesTableAdapter();
                aFindAllVehicleProblemStatusesTableAdapter.Fill(aFindAllVehicleProblemStatusesDataSet.FindAllVehicleProblemsForStatuses);
            }
            catch (Exception Ex)
            {
                TheEventLogClass.InsertEventLogEntry(DateTime.Now, "Correct Vehicle statuses // Find All Vehicle statuses " + Ex.Message);

                TheMessagesClass.ErrorMessage(Ex.ToString());
            }


            return aFindAllVehicleProblemStatusesDataSet;
        }

        private void BtnProcess_Click(object sender, RoutedEventArgs e)
        {
            string strProblemStatus;
            DateTime datResolutionDate;
            int intInvoiceID = -1;
            int intCounter;
            int intNumberOfRecords;
            bool blnFatalError = false;
            int intProblemID;
            bool blnProblemSolved;
            int intRecordsReturned;
            int intVehicleID;

            try
            {
                intNumberOfRecords = TheFindAllVehicleProblemStatusesDataSet.FindAllVehicleProblemsForStatuses.Rows.Count - 1;

                for(intCounter = 0; intCounter <= intNumberOfRecords; intCounter++)
                {
                    intProblemID = TheFindAllVehicleProblemStatusesDataSet.FindAllVehicleProblemsForStatuses[intCounter].ProblemID;
                    blnProblemSolved = TheFindAllVehicleProblemStatusesDataSet.FindAllVehicleProblemsForStatuses[intCounter].ProblemSolved;
                    intVehicleID = TheFindAllVehicleProblemStatusesDataSet.FindAllVehicleProblemsForStatuses[intCounter].VehicleID;

                    if(blnProblemSolved == true)
                    {
                        if(TheFindAllVehicleProblemStatusesDataSet.FindAllVehicleProblemsForStatuses[intCounter].IsInvoiceIDNull() == true)
                        {
                            datResolutionDate = FindResolutionDate(intProblemID);

                            blnFatalError = TheVehicleProblemClass.UpdateVehicleProblemResolution(intProblemID, datResolutionDate, gstrProblemResolution, intInvoiceID);

                            if (blnFatalError == true)
                                throw new Exception();

                            blnFatalError = TheVehicleProblemClass.ChangeVehicleProblemStatus(intProblemID, "CLOSED");

                            if (blnFatalError == true)
                                throw new Exception();
                        }
                        
                    }
                    else if(blnProblemSolved == false)
                    {
                        TheFindVehiclesInShopByVehicleIDDataSet = TheVehiclesInShopClass.FindVehiclesInShopByVehicleID(intVehicleID);

                        intRecordsReturned = TheFindVehiclesInShopByVehicleIDDataSet.FindVehiclesInShopByVehicleID.Rows.Count;

                        if(intRecordsReturned == 0)
                        {
                            blnFatalError = TheVehicleProblemClass.ChangeVehicleProblemStatus(intProblemID, "NEED WORK");

                            if (blnFatalError == true)
                                throw new Exception();
                        }
                        else if(intRecordsReturned > 0)
                        {
                            blnFatalError = TheVehicleProblemClass.ChangeVehicleProblemStatus(intProblemID, "NEED WORK");

                            if (blnFatalError == true)
                                throw new Exception();
                        }
                    }
                }

                TheMessagesClass.InformationMessage("The Process is Complete");

                TheFindAllVehicleProblemStatusesDataSet = FindAllVehicleProblemStatuses();

                dgrResults.ItemsSource = TheFindAllVehicleProblemStatusesDataSet.FindAllVehicleProblemsForStatuses;
            }
            catch(Exception Ex)
            {
                TheEventLogClass.InsertEventLogEntry(DateTime.Now, "Correct Vehicle Statuses // Process Button " + Ex.Message);

                TheMessagesClass.ErrorMessage(Ex.ToString());
            }
            
        }
        private DateTime FindResolutionDate(int intProblemID)
        {
            DateTime datResolutionDate = DateTime.Now;
            int intCounter;
            int intNumberOfRecords;

            try
            {
                TheFindVehicleMainProblemUpdateByProblemIDDataSet = TheVehicleProblemClass.FindVehicleMainProblemUpdateByProblemID(intProblemID);

                intNumberOfRecords = TheFindVehicleMainProblemUpdateByProblemIDDataSet.FindVehicleMainProblemUpdateByProblemID.Rows.Count - 1;

                for(intCounter = 0; intCounter <= intNumberOfRecords; intCounter++)
                {
                    if(intCounter == 0)
                    {
                        datResolutionDate = TheFindVehicleMainProblemUpdateByProblemIDDataSet.FindVehicleMainProblemUpdateByProblemID[0].TransactionDate;

                        gstrProblemResolution = TheFindVehicleMainProblemUpdateByProblemIDDataSet.FindVehicleMainProblemUpdateByProblemID[intCounter].ProblemUpdate;
                    }
                    else
                    {
                        if(datResolutionDate < TheFindVehicleMainProblemUpdateByProblemIDDataSet.FindVehicleMainProblemUpdateByProblemID[intCounter].TransactionDate)
                        {
                            datResolutionDate = TheFindVehicleMainProblemUpdateByProblemIDDataSet.FindVehicleMainProblemUpdateByProblemID[intCounter].TransactionDate;

                            gstrProblemResolution = TheFindVehicleMainProblemUpdateByProblemIDDataSet.FindVehicleMainProblemUpdateByProblemID[intCounter].ProblemUpdate;
                        }
                    }
                }
            }
            catch (Exception Ex)
            {
                TheEventLogClass.InsertEventLogEntry(DateTime.Now, "Correct Vehicle Statuses // Find Resolution Date " + Ex.Message);

                TheMessagesClass.ErrorMessage(Ex.ToString());
            }

            return datResolutionDate;
        }
    }
}
