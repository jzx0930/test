/*
This small ControllerLib shows a small part of
JobHandling and variable setting in the ARGES Firmware
using the the ARGES ControllerLib.
The project file is generated using Visual Studio 2019.

This example tries to connect to an ASC with the IP
172.29.227.238. Please adjust this IP in the code
to your needs.

This example shows:
- How to initialise the ControllerLib
- How to connect to an ASC 
- How to enable the variable cache
- How to load a job from the filesystem to the ASC
- How to set pen variables
- How to set job variables
- How to start a job
- How to wait until a job is finished (using a PLCChange-Callback)


The entry point is the main() function. This is also the place
to edit the IP of your ASC.

The job, which gets loaded, consists of a hatched ellipse. 
The x-position of the ellipse is moved from
0.0 to 10.0 and then to -10.0.
*/

#include <iostream>
#include <string>
#include "arg_controllerlib.h"


/***********************************************************/
/*
  Callbackfunctions

  These function get registered to the ControllerLib and
  get called, when special events occur. In the case of
  this example this are Errors and PLC-Changes. In bigger
  programs this might also be Variable-Value-Changes and
  so on.
*/
/***********************************************************/

/*
This functions gets called, when the ARGES ControllerLib detects
an error.
This function is registered using call to RegisterOnError
*/
int OnError(int errorcode, const char* description, HController HC) {
  std::cout << "Errorcode: " << errorcode << "  Desctiption: " << description
            << std::endl;
  printf("ErrorNr: %i, Description: %s\n", errorcode, description);

  return 0;
}


/*
Only for this example we use global variables 
to indicate, whether a job is active or ready.
This variables get set in the PLCChangeCallback.
*/
int g_jobactive = 0;
int g_jobready = 0;


/*
This function gets called, when the PLC-State changes 
on the controller.
It has to be registered with a call to
RegisterOnPlcChange()
*/
int PLCChangeCallback(HController HC, unsigned int value, unsigned int reserved) {
  if (value & (1 << PLC_DEVICES_READY | 1 << PLC_JOB_READY)) {
    g_jobactive = 0;
    g_jobready = 1;

  }
  if (value & (1 << PLC_JOB_ACTIVE)) {    
    g_jobactive = 1;
    g_jobready = 0;
  }

  return 0;
}


/*
Main program.
*/
int main(int argc, char *argv[]) 
{
  // This is the IP of our controller. Please change it to the IP of
  // your ASC
  const std::string DEFAULT_IP = "172.29.227.238";

  // This Port is the connection port of the ARGES Firmware.
  // Currently this has always to be 1610.
  const short PORT = 1610;


  /*
  When we have a parameter to this program, we assume it is
  an IP-Address.
  */
  std::string IP = DEFAULT_IP;
  if (argc == 2) {
    IP = argv[1];
  }

  /***********************************************************/
  /***                                                     ***/
  /*** Steps, every ControllerLib-Program needs            ***/
  /***                                                     ***/
  /***********************************************************/

  /*
  
  --- INITIALISATION OF THE ARGES CONTROLLERLIB
   
  A call to this method is mandatory. It initalises internal
  datastructures of the ControllerLib. Without a call to this
  method all other calls might have undefined behaviour.
  */
  InitControllerLib();

  
  
  /*   
  --- REGISTERING THE ERROR CALLBACK

  Registering an error callback is useful if the connection
  to the ASC gets lost or some other critical error occurs.

  This callback gets called on critical errors only.  
  */
  RegisterOnError(OnError);


  /*  
  --- CONNECTING TO AN ASC CONTROLLER

  With a call to DetectRemoteController you can connect to an
  ARGES ASC Controller. The ControllerLib can connect to any
  ASC Controller which uses Firmware 2 or Firmware 3. Most of
  the differences are handled internally.
  If no connection to controller can be established, the function
  returns the value ARG_INVALID_HANDLE_VALUE.
  Please note, that currently the PORT in this function has to
  be 1610 always.
  */
  std::cout << "Connecting to the controller " << IP << std::endl;
  HController HC = DetectRemoteController(IP.c_str(), PORT);
  if (HC == ARG_INVALID_HANDLE_VALUE) {
    std::cout << "Could not connect to controller " << IP << std::endl;
    DeinitControllerLib();
    return -1;
  }



  /*   
  --- Registering the PLC-Callback

  The PLC-Callback gets called, when the PLC-State on the controller
  changes. It is mainly used to see, if a job is running or can 
  be started.
  */
  RegisterOnPLCChanged(HC, PLCChangeCallback);


  /*
  
  ---ENABLING THE VARIABLE CACHE

  The variable cache mirrors the complete variabe tree of the Firmware
  locally. In InScript you can see this variable tree in the Inspector-View
  of the ASC.

  Even though a call to this function is not mandatory it is highly
  recommended to use this, because the handling of variable changes
  and so on is much easier in code.
  */ 
  std::cout << "Enabling the variable cache..." << std::endl;
  if (EnableVariableCache(HC) != E_OK) {
    std::cout << "Could not enable the variable cache..."
              << std::endl;
    DeinitControllerLib();
    return -1;
  }


  /***********************************************************/
  /***                                                     ***/
  /*** Setting Pen-Variables                               ***/
  /***                                                     ***/
  /***********************************************************/

  // This is the variable, where the linespeed of the default
  // pen can be set. To get the complete variable path, open
  // InScript, find the variable in the NodeProperties, 
  // right-click on the value of that variable and choose 
  // "Nodepath to clipboard". Then you can paste it.
  const std::string PEN_LINESPEED = "usr.pens.default.linepar.common.speed_m";

  
  // We try to find the variable in our cache (which we enabled with
  // EnableVariableChache). If we don't find it, we finish the program.
  // This variable has to exist always.  
  HNodeObject HNO_linespeed = GetNodeFromCache(HC, PEN_LINESPEED.c_str());
  if (HNO_linespeed == ARG_INVALID_HANDLE_VALUE) {
    std::cout << "Could not find node " << PEN_LINESPEED << std::endl;
    DeinitControllerLib();
    return -1;
  }

  // We set the local value of the linespeed to 100.0 mm/sec.
  // Note, that this only changes the local value, but is not
  // transferred to controller yet!
  SetNodeValueReal32(HC, HNO_linespeed, 100.0f); 

  // With this call, we tell the Firmware on the controller
  // the new value.
  WriteNode(HC, HNO_linespeed);

  
  /***********************************************************/
  /***                                                     ***/
  /*** Loading a job                                       ***/
  /***                                                     ***/
  /***********************************************************/

  // This is the complete path to the job
  const std::string JOBPATH("..\\Jobs\\FilledCircle.jobx");

  // This indicates, if loading the job should clear all
  // other jobs on the Controller first.
  // If set to 1, all other jobs will be deleted,
  // if set to 0, the jobs remain on the controller
  const int CLEARFIRST = 1;

  // This indicates, if the job should be selected after it is loaded.
  // This means, after loading the job, you can wait for the PLC-State
  // to become JOB_READY. After that you can start the job.
  const int SELECTJOB = 1;

  // Loading the job
  if (LoadJob(HC, JOBPATH.c_str(), CLEARFIRST, SELECTJOB) != E_OK) {
    std::cout << "Could not load job" << std::endl;
    DeinitControllerLib();
    return -1;
  }

  

  // After loading the job, we wait until the job is selected and
  // ready for output.
  std::cout << "Waiting for the job to become ready." << std::endl;
  while (g_jobready == 0) {
    std::cout << ".";
    std::cout.flush();
    Sleep(10);
  }
  std::cout << std::endl;

  

  float xpositions[3];
  xpositions[0] = 0.0;
  xpositions[1] = 10.0;
  xpositions[2] = -10.0;


 
  for (int jobrun = 0; jobrun < 3; ++jobrun) {
    // This is the variable, where the x-Position is stored.
    const std::string VAR_XPOS = "usr.job.FilledCircle.Hatch.Ellipse..x1";


    std::cout << std::endl
              << "Setting the X-Position to " << xpositions[jobrun]
              << std::endl;
    // We get the variable from the cache (like the linespeed variable above)...
    HNodeObject HNO_XPos = GetNodeFromCache(HC, VAR_XPOS.c_str());
    if (HNO_XPos == ARG_INVALID_HANDLE_VALUE) {
      std::cout << "Could not find node " << VAR_XPOS << std::endl;
      DeinitControllerLib();
      return -1;
    }

    //...set the local value to the correct x position...
    SetNodeValueReal32(HC, HNO_XPos, xpositions[jobrun]); 

    //...and write it to the firmware.
    WriteNode(HC, HNO_XPos);

    // Now we can start the job
    std::cout << "Starting the currently selected job." << std::endl;
    JobStart(HC);

    // We wait for the job to become active
    std::cout << "Waiting for the job to become active." << std::endl;
    while (g_jobactive == 0) {
      std::cout << ".";
      std::cout.flush();
      Sleep(10);
    }
    std::cout << std::endl;

    // And when the job is active, we wait until
    // the job is done (meaning; the job is ready
    // for output again.
    std::cout << "Waiting for the job to finish." << std::endl;
    while (g_jobready == 0) {
      std::cout << ".";
      std::cout.flush();
      Sleep(10);
    }
    std::cout << std::endl;

    std::cout << "Job finished." << std::endl;  
  }
 
  
  /*
  --- CLEANUP OF THE CONTROLLERLIB

  This cleans up the ControllerLib structures and closes all
  connections to all Controllers.
  */
  DeinitControllerLib();
}