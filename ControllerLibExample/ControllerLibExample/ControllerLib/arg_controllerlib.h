/*
 * Copyright (C) Arges GmbH
 *       www.arges.de
 */

/* *********************************** */
/* *********************************** */
/*  THIS FILE IS GENERATED.            */
/*  CHANGES IN THIS FILE WILL BE       */
/*  OVERWRITTEN                        */
/* *********************************** */
/* *********************************** */

#ifndef __ARG_CONTROLLER_LIBC__
#define __ARG_CONTROLLER_LIBC__

#ifdef _WIN32
   //We are on Windows
#  ifndef WIN32_LEAN_AND_MEAN
#    define WIN32_LEAN_AND_MEAN
#  endif
#  ifdef _WIN64
     //We are on x64
#    ifndef WIN64
#      define WIN64
#    endif
#  else // _WIN64
     // We are on x86
#    ifndef WIN32
#      define WIN32
#    endif
#  endif // _WIN64
#else // _WIN32
  // We're not on Windows, maybe WindowsCE or WindowsPhone stuff, otherwise some other platform
#endif

#if defined(WIN32) || defined(WIN64) || defined(WINCE)
#  ifdef __cplusplus
#    define EXPORTDLL extern "C" __declspec (dllexport)
#  else
#    define EXPORTDLL __declspec (dllexport)
#  endif
#endif

#if defined(WINCE)
#  ifndef WIN32_LEAN_AND_MEAN
#    define WIN32_LEAN_AND_MEAN
#  endif
#endif

#if defined(WIN32) || defined(WIN64) || defined(WINCE)
#  include <windows.h>
#  include <winsock2.h>
#endif

#ifndef EXPORTDLL
#  define EXPORTDLL
#endif

#ifndef __CONTROLLER_LIB_H_INCLUDED__
typedef enum
{
  E_OK = 0,
  E_RANGE = -1,
  E_UNAVAIL = -2,
  E_NOSPACE = -3,
  E_INVALID = -4,
  E_CRC = -5,
  E_NOMEM = -6,
  E_TIMEOUT = -7,
  E_BREAK = -8,
  E_BUSY = -9,
  E_UNIMP = -10,
  E_EXIST = -11,
  E_NOEXIST = -12,
  E_ACCESS = -13,
  E_NOT_RDY = -14,
  E_FAILURE = -15,
  E_RETRY = -16,
  E_NFILE = -17,
  E_NALLOWED = -18
}
STATUSCODES;


typedef enum
{
  NODESTATE_UNDEFINED = 0,
  NODESTATE_CREATING,
  NODESTATE_UNINITIALIZED,
  NODESTATE_VALUEVALID,
  NODESTATE_VALUEINVALID,
  NODESTATE_CLIENTCHANGING,
  NODESTATE_CLIENTVALIDATING,
  NODESTATE_CONTROLLERVALIDATING,
  NODESTATE_CONTROLLERCHANGING,
  NODESTATE_VALUEACCEPTED,
  NODESTATE_DELETING
}
NODESTATES;

typedef enum
{
  PLC_DEVICES_AWAKE = 0,
  PLC_DEVICES_FAILURE = 1,
  PLC_DEVICES_READY = 2,
  PLC_DEVICES_SETUP = 3,
  PLC_JOB_READY = 4,
  PLC_JOB_ACTIVE = 5,
  PLC_JOB_COMPLETED = 6,
  PLC_JOB_FAILED = 7,
  PLC_SYS_SAFE = 8,
  PLC_JOB_PAUSED = 9,
  PLC_JOB_PILOTED = 10,
  PLC_JOB_PRELOADED = 11,
  PLC_JOB_STOPPING = 12,
  PLC_READY_FOR_POWER_OFF = 13,
  PLC_SYSTEM_READY = 14,
  PLC_ATTENTION = 15,
  PLC_SYS_SAFE_REQUEST = 16,
  PLC_JOB_PRELOAD  = 17
}
PLCSTATES;


typedef enum
{
  ERROR_CONNECTION_LOST = 1,
  ERROR_OCCUPIED = 2,
  ERROR_COULD_NOT_OPEN_FILE = 3,
  ERROR_CORRUPTED_DATA = 4
}
ERRORCODES;

typedef enum
{
  ARG_LOG_SUCCESS,
  ARG_LOG_INFO,
  ARG_LOG_WARNING,
  ARG_LOG_ERROR,
} ARG_LOG_LEVEL;

#if !defined(_WIN32)
typedef long DWORD;
#endif

#if !defined(ARG_INVALID_HANDLE_VALUE)
#  define ARG_INVALID_HANDLE_VALUE (DWORD)0xffffffff
#endif

#if !defined(INVALID_HANDLE_VALUE)
#  define INVALID_HANDLE_VALUE ARG_INVALID_HANDLE_VALUE
#endif

#ifdef _WIN32
typedef __int64 int64;
typedef unsigned __int64 uint64;
#else
typedef long long int64;
typedef unsigned long long uint64;
#endif

typedef DWORD HAnything;
typedef DWORD HNodeObject;
typedef DWORD HNodeObjectCollection;
typedef DWORD HController;
typedef DWORD HSysMsg;
typedef DWORD HHead;
typedef DWORD HScanfieldCorrection;
typedef DWORD HContext;
typedef DWORD HDevice;

typedef enum
{
  VT_INV = 0,                   //  0 invalid
  VT_I32,                       //  1 INT32
  VT_I64,                       //  2 INT64
  VT_R32,                       //  3 REAL32
  VT_R64,                       //  4 REAL64
  VT_BOOLEAN,                   //  5 BOOLEAN
  VT_STR,                       //  6 STRING
  VT_SEL,                       //  7 SELECT
  VT_SET,                       //  8 SET
  VT_BIN,                       //  9 BINARY
  VT_TEXT,                      // 10 TEXT
  VT_FILE,                      // 11 FILE
  VT_JOB,                       // 12 Jobnode
  VT_LAST_
}
VAR_TYPE;

typedef enum
{
  ARG_DEV_UNINITIALIZED,
  ARG_DEV_INACTIVE,
  ARG_DEV_ACTIVE,
  ARG_DEV_ACTIVATING,
  ARG_DEV_DEACTIVATING,
  ARG_DEV_ACTIVATIONERROR,
  ARG_DEV_DEACTIVATIONERROR,
  ARG_DEV_UNDEFINED
} ARG_DEVICE_ACTIVATION_STATE;

typedef enum
{
  ARG_DEV_ERR_UNINITIALIZED,
  ARG_DEV_ERR_OK,
  ARG_DEV_ERR_RECOVER,
  ARG_DEV_ERR_RECOVERERROR,
  ARG_DEV_ERR_RESET,
  ARG_DEV_ERR_RESETERROR,
  ARG_DEV_ERR_FAILURE,
  ARG_DEV_ERR_NOTREADY,
  ARG_DEV_ERR_UNDEFINED
} ARG_DEVICE_ERROR_STATE;


typedef enum
{
  ARG_DEV_PARAM_UNINITIALIZED,
  ARG_DEV_PARAM_IDLE,
  ARG_DEV_PARAM_EVAL,
  ARG_DEV_PARAM_REEVAL,
  ARG_DEV_PARAM_PRESUSPEND,
  ARG_DEV_PARAM_SUSPENDED,
  ARG_DEV_PARAM_UNDEFINED
} ARG_DEVICE_PARAM_STATE;

typedef enum
{
  ARG_DEV_POWER_UNINITIALIZED,
  ARG_DEV_POWER_CHECKREADYTOSTANDBY,
  ARG_DEV_POWER_EMERGENCYOFF,
  ARG_DEV_POWER_DOWN,
  ARG_DEV_POWER_GOINGDOWNTOSTANDBY,
  ARG_DEV_POWER_GOINGDOWNTOOFF,
  ARG_DEV_POWER_GOINGOFFTODOWN,
  ARG_DEV_POWER_GOINGREADYTOSTANDBY,
  ARG_DEV_POWER_GOINGSTANDBYTODOWN,
  ARG_DEV_POWER_GOINGSTANDBYTOREADY,
  ARG_DEV_POWER_OFF,
  ARG_DEV_POWER_READY,
  ARG_DEV_POWER_STANDBY,
  ARG_DEV_POWER_UNDEFINED
} ARG_DEVICE_POWER_STATE;

struct ARG_LINEINFO {
  HNodeObject  HNO;
  int          includessubnodes;
  int          packagenumber;
  int          lastpackage;
  DWORD        userhandle;
  unsigned int datalength;
  void        *data;
};

struct ARG_TSS_DATA
{
  int connectionid;
  char *channelname;
  char *channelspecifier;
  int blocknumber;
  int totalblockcount;
  int aborted; /* 1 = aborted, 0 = not aborted */
  int timestampInt;
  unsigned long long starttimestampInt;
  unsigned long long endtimestampInt;
  double starttimestamp;
  double endtimestamp;
  int samplespersec;
  unsigned int datalen;
  const void *data;
};

struct ARG_GRID_DATA
{
  int xNum;     /* Number of x-points */
  int yNum;     /* Number of y-points */
  int zNum;     /* Number of z-points */
  int sfNum;    /* Number of subframes in the binary data */
  double xMin;  /* Minimum of the x-axis */
  double yMin;  /* Minimum of the y-axis */
  double zMin;  /* Minimum of the z-axis */
  double xMax;  /* Maximum of the x-axis */
  double yMax;  /* Maximum of the y-axis */
  double zMax;  /* Maximum of the z-axis */
  unsigned int datalen; /* Size of the data in bytes */
  void *data;       /* Data */
};

#endif // __CONTROLLER_LIB_H_INCLUDED__
typedef enum {
  NODEINFO_FULLNAME   =   1,
  NODEINFO_NAME       =   2,
  NODEINFO_PATH       =   4,
  NODEINFO_VALUE      =   8,
  NODEINFO_PRIV       =  16,
  NODEINFO_UNIT       =  32,
  NODEINFO_TYPESTRING =  64,
  NODEINFO_ALL        = 127
}
ARG_NODEINFO_FLAGS;


// This structure can be filled with
// GetNodeInfo
struct ARG_NODEINFO {
  HNodeObject   handle;     // The Handle of the NodeObject
  long long     nodeid;     // The ID on the controller
  char         *fullname;   // Full qualified name (e.g. "stat.time.TimeStr")
  char         *name;       // Only the last name of the variable
  char         *path;       // The path of the variable
  char         *value;      // Value as String
  char         *priv;       // Used for VAR:SEL to hold the different options
  char         *unit;       // The (optional) Unit of the NodeObject
  int           type;       // Type of the Variable
  char         *typestring; // Type as string (eg. "VAR:SET", "JOB:ROTATE"....)
  float         min;        // Minimum of the NodeObject
  float         max;        // Maximum of the NodeObject
  unsigned int  index;      // The index of the NodeObject in the tree (to sort)
  unsigned int  flags;      // Flags of the NodeObject (only for debugging)
};

struct ARG_JOBNODEINFO {
  char *fullname;
  char *name;
  char *classification;
  int   usercreatable;
  int   canmakelines;
  int   canpasslines;
  int   acceptssubnodes;
};

struct ARG_JOBINFO {
  int    jobcount;
  char **fulljobnames;
  char **jobnames;
  char  *selectednode;
};

struct ARG_SINGLEDRIVERINFO {
  char *name;
  char *version;
  char *vendor;
  char *comment;
  char *classification;
};

struct ARG_DRIVERINFO {
  int                    drivercount;
  ARG_SINGLEDRIVERINFO **driver;
};

struct ARG_FONTINFO {
  int fontcount;
  char **fontnames;
};


struct ARG_SINGLEDEVICEINFO {
  HDevice  handle;
  char    *name;
  char    *driver;
  char    *dependenciesvarname;
  int      dependenciescount;
  char   **dependencies;
  int      dependsoncount;
  char   **dependson;
  ARG_DEVICE_ACTIVATION_STATE activationstate;
  ARG_DEVICE_ERROR_STATE      errorstate;
  ARG_DEVICE_PARAM_STATE      paramstate;
  ARG_DEVICE_POWER_STATE      powerstate;
};

struct ARG_DEVICEINFO {
  int                    devicecount;
  ARG_SINGLEDEVICEINFO **device;
};


struct ARG_FEATURE {
  char *name;
  int   version;
  int   flags;
};

struct ARG_FEATURELIST {
  int           featurecount;
  ARG_FEATURE **feature;
};

struct ARG_TSS_CHANNELTYPE
{
  char *name;
  char *coordname;
  int axiscount;
  char **axisname;
};

// TimedSignalStream
struct ARG_TSS_INFO {
  int                   channelcount;
  ARG_TSS_CHANNELTYPE **channeltype;
};



/**
 * Callbackdefinition for ValueChangeListeners.
 */
typedef int (* ValueChangeCallbackFunction)    (HController HC, HNodeObject HNO);
typedef int (* ValueChangeCallbackFunctionExt) (HController HC, HNodeObject HNO, void *userpointer);

typedef int (* FlagsChangeCallbackFunction)    (HController HC, HNodeObject HNO);
typedef int (* FlagsChangeCallbackFunctionExt) (HController HC, HNodeObject HNO, void *userpointer);

typedef int (* NameChangeCallbackFunction)    (HController HC, HNodeObject hNodeObject);
typedef int (* NameChangeCallbackFunctionExt) (HController HC, HNodeObject hNodeObject, void *userpointer);

typedef int (* NodeCreatedCallbackFunction)    (HController HC, const char* varname);
typedef int (* NodeCreatedCallbackFunctionExt) (HController HC, const char* varname, void *userpointer);
typedef int (* StartOfNodeCreatedRequestCallbackFunction)   (HController HC, void *userpointer);
typedef int (* EndOfNodeCreatedRequestCallbackFunction)     (HController HC, void *userpointer);

typedef int (* NodeDeletedCallbackFunction)    (HController HC, HNodeObject HNO);
typedef int (* NodeDeletedCallbackFunctionExt) (HController HC, HNodeObject HNO, void *userpointer);
typedef int (* StartOfNodeDeletedRequestCallbackFunction)   (HController HC, void *userpointer);
typedef int (* EndOfNodeDeletedRequestCallbackFunction)     (HController HC, void *userpointer);


typedef int (* NodeModifiedCallbackFunction) (HController HC,
                                              HNodeObject HNO,
                                              void *userpointer,
                                              int reserved);

typedef int (* NodeMovedCallbackFunction) (HController HC,
                                           HNodeObject HNO,
                                           HNodeObject FormerParent,
                                           HNodeObject NewParent,
                                           void *userpointer);

typedef int (* NodeMovedCallbackFunctionExt) (HController HC,
                                           HNodeObject HNO,
                                           HNodeObject FormerParent,
                                           HNodeObject NewParent,
                                           int FormerIndex,
                                           int NewIndex,
                                           void *userpointer);

typedef int (* NodeStateChangeCallbackFunction) (HController HC,
                                                  HNodeObject HNO,
                                                  int newstate,
                                                  int oldstate,
                                                  int error,
                                                  void *userpointer);


typedef int (* PLCChangeCallbackFunction)    (HController HC, unsigned int value, unsigned int reserved);
typedef int (* PLCChangeCallbackFunctionExt) (HController HC, unsigned int value, unsigned int reserved, void *userpointer);

typedef int (* SysMsgCallbackFunction)    (HController HC, HSysMsg HSM); // deprecated
typedef int (* SysMsgCallbackFunctionExt) (HController HC, HSysMsg HSM, void *userpointer);  // deprecated
typedef int (* SystemMessageCallbackFunction) (HSysMsg HSM, void *userpointer);

typedef int (* RequestLogCallbackFunction) (HController HC, const char* logentry, void *userpointer);

typedef int (* ErrorCallbackFunction)    (int errorcode, const char* description, HController HC);

typedef int (* LogCallbackFunction) (ARG_LOG_LEVEL level, const char *datetime, const char* logentry, void *userpointer);

typedef int (* DeviceCreatedCallbackFunction) (HController HC, HDevice HD, void *userpointer);

typedef int (* DeviceDeletedCallbackFunction) (HController HC, HDevice HD, void *userpointer);

typedef int (* DeviceActivatedCallbackFunction) (HController HC, HDevice HD, void *userpointer);

typedef int (* DeviceDeactivatedCallbackFunction) (HController HC, HDevice HD, void *userpointer);

typedef int (* DeviceDependencyAddedCallbackFunction) (HController HC, HDevice ParentDevice, HDevice SubDevice, const char *depvar,  void *userpointer);

typedef int (* DeviceDependencyRemovedCallbackFunction) (HController HC, HDevice ParentDevice, HDevice SubDevice, const char *depvar, void *userpointer);

typedef int (* DeviceStateChangedCallbackFunction) (HController HC, HDevice HD, ARG_DEVICE_ACTIVATION_STATE oldstate, ARG_DEVICE_ACTIVATION_STATE newstate, void *userpointer);

typedef int (* DeviceErrorStateChangedCallbackFunction) (HController HC, HDevice HD, ARG_DEVICE_ERROR_STATE oldstate, ARG_DEVICE_ERROR_STATE newstate, void *userpointer);

typedef int (* DeviceParamStateChangedCallbackFunction) (HController HC, HDevice HD, ARG_DEVICE_PARAM_STATE oldstate, ARG_DEVICE_PARAM_STATE newstate, void *userpointer);

typedef int (* DevicePowerStateChangedCallbackFunction) (HController HC, HDevice HD, ARG_DEVICE_POWER_STATE oldstate, ARG_DEVICE_POWER_STATE newstate, void *userpointer);

typedef int (* JobLinesCallbackFunction) (HController HC, ARG_LINEINFO *lineinfo);

typedef int (* TssDataCallbackFunction)(HController HC, ARG_TSS_DATA *tssdata, void *userpointer);

typedef int (* UpdateTssChannelsCallbackFunction)(HController HC, void *userpointer);


/* =========================================== */
/*     Names of all known Jobnodetypes         */
/* =========================================== */


#define ARG_JNT_POSITION_ENCODER  "OnTheFly"
#define ARG_JNT_ON_THE_FLY        "OnTheFly"
#define ARG_JNT_TRANSFORM         "Transform2D"
#define ARG_JNT_TRANSFORM3D       "Transform3D"
#define ARG_JNT_SCALEPRECESSION   "ScalePrecession"

// Drawing Nodes
#define ARG_JNT_BARCODE      "Barcode"
#define ARG_JNT_BARCODE_2D   "Barcode2D"
#define ARG_JNT_BITMAP       "Bitmap"
#define ARG_JNT_ROUNDTEXT    "CircularText"
#define ARG_JNT_CONCCIRCLES  "ConcentricCircles"
#define ARG_JNT_DOT          "Dot"
#define ARG_JNT_ELLIPSE      "Ellipse"
#define ARG_JNT_FILL         "Hatch"
#define ARG_JNT_LINE         "Line"
#define ARG_JNT_SPIRO3D      "PrecessionDrill"
#define ARG_JNT_RAWLINES     "RawLines"
#define ARG_JNT_RECTANGLE    "Rectangle"
#define ARG_JNT_POLYGON      "RegularPolygon"
#define ARG_JNT_SPIRO        "Spiral"
#define ARG_JNT_SPLINE       "Spline"
#define ARG_JNT_SPLIT_TEXT   "SplitText"
#define ARG_JNT_TEXT         "Text"
#define ARG_JNT_CUTTINGEDGE  "CuttingEdge"
#define ARG_JNT_RAWDRILL     "RawDrill"

// Internal Nodes
#define ARG_JNT_QUEUE_COPYMEM  "QueueCopyMemory"
#define ARG_JNT_QUEUE_DELAY    "QueueDelay"
#define ARG_JNT_QUEUE_POS_ENC  "QueuePosEnc"
#define ARG_JNT_QUEUE_SET_DAC  "QueueSetDac"
#define ARG_JNT_QUEUE_WAIT     "QueueWait"
#define ARG_JNT_TESTMARK       "Testmark"
#define ARG_JNT_TESTPAT        "TestPatterns"

//Fixed SubNodes
#define ARG_JNT_POSLIST_LS      "AlignmentPath"
#define ARG_JNT_POSLIST_DR      "ObjectAlongPath"
#define ARG_JNT_TILING_SRC      "Source"
#define ARG_JNT_TILE_S          "Tile"
#define ARG_JNT_TILING_TLS      "Tiles"
#define ARG_JNT_TILING_PREPOST  "PrePost"
#define ARG_JNT_TILE_SET        "TileSet"

//Organizing Nodes
#define ARG_JNT_JOB          "Job"
#define ARG_JNT_SCAN3D       "Scan3D"
#define ARG_JNT_CONDITIONAL  "Conditional"
#define ARG_JNT_COUNTER      "Counter"
#define ARG_JNT_DELAY        "Delay"
#define ARG_JNT_FLUSH        "Flush"
#define ARG_JNT_COLLECT      "Group"
#define ARG_JNT_PENSET       "PenMap"
#define ARG_JNT_SCRIPT       "Script"
#define ARG_JNT_PEN          "UsePen"

// Repeating Nodes
#define ARG_JNT_REPEAT       "Loop"
#define ARG_JNT_CLOCK        "RepeatCircular"
#define ARG_JNT_POSLIST      "RepeatAlongPath"
#define ARG_JNT_MAT2OFF      "RepeatXY"
#define ARG_JNT_EXT_REP      "AxisRepeat"
#define ARG_JNT_EXT_POS      "MoveAxes"
#define ARG_JNT_EXT_M2O      "AxesRepeatXY"
#define ARG_JNT_TILING       "Tiling"

//IOs
#define ARG_JNT_QUERY         "AccessDataBase"
#define ARG_JNT_FRAMEGRABBER  "AcquireImage"
#define ARG_JNT_MEMO          "Comment"
#define ARG_JNT_INPUT         "EditValue"
#define ARG_JNT_EXT_SELECT    "ExternalSelect"
#define ARG_JNT_HOST_EXEC     "HostCommand"
#define ARG_JNT_INFO          "MessageDialog"
#define ARG_JNT_VISION        "ProcessImage"
#define ARG_JNT_EXT_CMD       "SendRS232Command"
#define ARG_JNT_SIGWAIT       "SignalWait"

// To be deleted
//#define ARG_JNT_SHAPE        "SHAPE"
//#define ARG_JNT_REP_X        "REP_X"
//#define ARG_JNT_REP_Y        "REP_Y"
//#define ARG_JNT_LINK         "LINK"
//#define ARG_JNT_SLOT         "SLOT"
//#define ARG_JNT_STOP         "STOP"
//#define ARG_JNT_ROTATE       "ROTATE"
//#define ARG_JNT_SCALE        "SCALE"
//#define ARG_JNT_SLANT        "SLANT"
//#define ARG_JNT_OFFSET       "OFFSET"
//#define ARG_JNT_ROTATE3D     "ROTATE3D"
//#define ARG_JNT_OFFSET3D     "OFFSET3D"
//#define ARG_JNT_SPLIT        "SPLIT"

#define ARG_JNT_DFONT  "DFONT"

extern "C" {


/**
 * Initializes the library. 
 * 
 * This function has to be called before any other call to the library. Otherwise any calls to the library will not work. 
 * 
 * Directly after this call we recommend to call \nameref{fct:RegisterOnError}.
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:DeinitControllerLib
 * \see \fullref{fct:RegisterOnError
 * \see \fullref{fct:GetControllerLibVersionString
 * \see \fullref{fct:IsInitialized
 */
EXPORTDLL int InitControllerLib(void);

/**
 * Deinitializes the library. 
 * 
 * This function has to be called before the client application exits.
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:InitControllerLib
 * \see \fullref{fct:IsInitialized
 */
EXPORTDLL int DeinitControllerLib(void);

/**
 * Returns, whether the library is already initialized or not.
 *
 * \return
 *     1 if the library is initialized
 *     0 if the library is not initialized
 *
 * \see \fullref{fct:InitControllerLib
 * \see \fullref{fct:DeinitControllerLib
 */
EXPORTDLL int IsInitialized(void);

/**
 * Gets the version of the library as a string.
 *
 * \return
 *     
 *
 * \see \fullref{fct:InitControllerLib
 */
EXPORTDLL const char* GetControllerLibVersionString(void);

/**
 * Frees memory allocated by the ControllerLib as the function \texttt{free()} cannot free this memory due to ownership issues on the \Windows\ operating system.
 *
 * \see \fullref{fct:GetPenPath
 * \see \fullref{fct:GetFontPath
 */
EXPORTDLL void CLFree(void *pointer);

/**
 * The firmware relies on a period (dot) as decimal separator when floating point numbers are passed as strings. When calling the functions \nameref{fct:GetNodeValueString}, \nameref{fct:SetNodeValueString}, \nameref{fct:GetNodeInfo} or \nameref{fct:GetNodeInfoExt} these will automatically correct accidental commas into periods (dots) in the string representations of floating point numbers. Calling function DisableFloatingPointToStringDot disables this behavior and is not recommended.
 *
 * \see \fullref{fct:SetNodeValueString
 * \see \fullref{fct:GetNodeValueString
 * \see \fullref{fct:GetNodeInfo
 * \see \fullref{fct:GetNodeInfoExt
 */
EXPORTDLL void DisableFloatingPointToStringDot(void);

/**
 * This function tells the controller's firmware the name of the client application.
 * 
 * As the string is initially empty the function should be called before calling function \nameref{fct:DetectRemoteController}.
 *
 * \param applicationname is the name of the client application
 *
 * \see \fullref{fct:SetApplicationVersion
 * \see \fullref{fct:GetApplicationName
 * \see \fullref{fct:SetFullFeatured
 * \see \fullref{fct:DetectRemoteController
 */
EXPORTDLL void SetApplicationName(const char *applicationname);

/**
 * This function tells the controller's firmware the version of the client application.
 * 
 * The function should be called before calling function \nameref{fct:DetectRemoteController}.
 *
 * \param major major nr of the application
 * \param minor minor nr of the application
 * \param modification modification nrof the application
 * \param buildnr buildnr of the application
 * \param versiontype versiontype version of the application (e.g. debug, TP, Release...)
 *
 * \see \fullref{fct:SetApplicationName
 * \see \fullref{fct:SetFullFeatured
 * \see \fullref{fct:DetectRemoteController
 */
EXPORTDLL void SetApplicationVersion(int major, int minor, int modification, int buildnr, char *versiontype);

/**
 * Gets the name of the application that was set by using \nameref{fct:SetApplicationName}.
 *
 * \return
 *     name of the application
 *
 * \see \fullref{fct:SetApplicationName
 * \see \fullref{fct:IsFullFeatured
 */
EXPORTDLL const char* GetApplicationName(void);

/**
 * Sets, whether the client application is \emph{fully featured} or not. Fully featured means, that the controller can ask the application for additional data, e.g. pens, bitmaps. By default the application is assumed as fully featured. 
 * 
 * This function must be called before calling function \nameref{fct:DetectRemoteController}.
 *
 * \param flag sets the client application as fully featured (texttt1
 *
 * \see \fullref{fct:IsFullFeatured
 * \see \fullref{fct:SetApplicationName
 * \see \fullref{fct:DetectRemoteController
 */
EXPORTDLL void SetFullFeatured(int flag);

/**
 * Returns, whether the client application is fully featured. This means, that the controller can ask the application for additional data, e.g.~pens, bit\-maps. By default the application is assumed as fully featured. 
 *
 * \return
 *     1 if the application is fully featured
 *     0 if the application is not fully featured
 *
 * \see \fullref{fct:SetFullFeatured
 * \see \fullref{fct:SetApplicationName
 */
EXPORTDLL int IsFullFeatured(void);

/**
 * This function tells the ControllerLib to use attributes.
 * 
 * This function has to be called before calling \nameref{fct:DetectRemoteController}.
 *
 * \see \fullref{fct:SetInternalAttributes
 * \see \fullref{fct:DetectRemoteController
 */
EXPORTDLL void SetAttributes(void);

/**
 * This function tells the ControllerLib to use firmware-internal attributes. The function is only used for debugging purposes.
 * 
 * This function has to be called before calling function \nameref{fct:DetectRemoteController}.
 *
 * \see \fullref{fct:SetAttributes
 * \see \fullref{fct:DetectRemoteController
 */
EXPORTDLL void SetInternalAttributes(void);

/**
 * Probes, whether a controller can be accessed at the given IP-address. The probing can last up to 5~seconds.
 *
 * \param host is the host name or IP-address of the remote controller, e.g.,enquotelstinlineascstack
 * \param port is the port of the remote controller, usually enquotelstinline1610
 *
 * \return
 *     1 if a controller has been found
 *     0 if a controller has not been found
 *
 * \see \fullref{fct:DetectRemoteController
 */
EXPORTDLL int ProbeRemoteController(const char  *host, short port);

/**
 * Detects, whether a controller can be accessed at the given IP-address and, if this is true, establishes a connection and returns a handle.
 *
 * \param host is the host name or IP-address of the remote controller, e.g.,enquotelstinlineascstack
 * \param port is the port of the remote controller, usually enquotelstinline1610
 *
 * \return
 *     handle to the controller
 *     ARG_INVALID_HANDLE_VALUE if a controller has not been found
 *
 * \see \fullref{fct:ProbeRemoteController
 * \see \fullref{fct:DisconnectController
 * \see \fullref{fct:EnableVariableCache
 */
EXPORTDLL HController DetectRemoteController(const char  *host, short port);

/**
 * This function returns the features supported by the InScript firmware. This function always returns a valid pointer which never is \texttt{NULL}. If no features are supported by the InScript firmware then the feature count is \texttt{0}. The returned value has to be destroyed by using function \nameref{fct:DestroyFeatureList}. 
 * \begin{lstlisting}[caption={GetFeatureList example}]
 * ARG_FEATURELIST *list = GetFeatureList(HC);
 * for (int i=0; i<list->featurecount; ++i) {
 *   printf("Feature: %s  (Version: %i, Flags: %x)\n", 
 *               list->feature[i]->name, 
 *               list->feature[i]->version, 
 *               list->feature[i]->flags
 *         );
 * }
 * DestroyFeatureList(list);
 * \end{lstlisting}
 *
 * \param HC is the handle to the controller
 *
 * \return
 *     ARG_FeatureList for the controller
 *
 * \see \fullref{fct:DestroyFeatureList
 */
EXPORTDLL ARG_FEATURELIST* GetFeatureList(HController HC);

/**
 * Returns, whether the firmware on the given controller supports jobs in the XML-format or not. 
 * \begin{lstlisting}[caption={SupportsXMLJobFormat example}]
 * int version, flags;
 * if ( SupportsXMLJobFormat(HC, &version, &flags) == E_OK) {
 *   if ( version == 1 ) {
 *     // Load job in XML-format version 1 or higher
 *   }
 * } else {
 *   // Load job in "oldstyle"-format
 * }
 * \end{lstlisting}
 *
 * \param HC is the handle to the controller
 * \param version holds the highest supported version if the function returns textttE_OK
 * \param flags holds additional information if the function returns textttE_OK
 *
 * \return
 *     E_OK if the firmware supports jobs in XML-format
 *     E_UNAVAIL if the firmware does not support jobs in XML-format
 *     E_FAILURE on error
 *
 * \see \fullref{fct:DestroyFeatureList
 */
EXPORTDLL int SupportsXMLJobFormat(HController HC, int *version, int *flags);

/**
 * Releases the memory obtained by the call to \nameref{fct:GetFeatureList}.
 *
 * \param list is a pointer to an ARG_FEATURELIST structure
 *
 * \see \fullref{fct:GetFeatureList
 */
EXPORTDLL void DestroyFeatureList(ARG_FEATURELIST *list);

/**
 * Mirrors all variables on the controller to the ARG_ControllerLib. New variables will be added automatically, deleted variables will be deleted in the ARG_ControllerLib as well. Registered callbacks will also work. It is also possible to traverse through the tree with \nameref{fct:GetParentNode} and \nameref{fct:GetSubnodes}.
 * 
 * This method is preferred if many variables are used by the client application. It is always advisable to enable the variable cache to gain performance.
 * 
 * This function should be called immediately after \nameref{fct:DetectRemoteController}.
 * 
 * \tip{See section \ref{sec:Using the VariableCache} for an example of how to use the VariableCache.}
 *
 * \param HC is the handle to the controller
 *
 * \return
 *     E_OK on success
 *     E_EXIST if the variable cache is already enabled
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:DetectRemoteController
 * \see \fullref{fct:GetParentNode
 * \see \fullref{fct:GetNodeFromCache
 * \see \fullref{fct:GetSubnodesCount
 * \see \fullref{fct:GetSubnodes
 */
EXPORTDLL int EnableVariableCache(HController HC);

/**
 * Gets the number of connected controllers.
 *
 * \return
 *     number of connected controllers
 *
 * \see \fullref{fct:DetectRemoteController
 */
EXPORTDLL int ControllerCount(void);

/**
 * Please note, that this function may take several minutes to return.
 * \todo{MR: Prima, aber was macht die Funktion?}
 *
 * \return
 *     E_OK on success
 *     E_NOMEM if the data is textttNULL
 *     E_UNIMP if the firmware does not support this feature
 *     E_FAILURE on failure
 */
EXPORTDLL int GetCorrectionGrid(HController HC, ARG_GRID_DATA *data);

/**
 * Frees the used internal correction data.
 * \todo{MR: Vielleicht sollte "man" correction etwas näher spezifizieren. 2 oder 3 erklärende Worte würden reichen.}
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 */
EXPORTDLL int FreeCorrectionGridData(ARG_GRID_DATA *data);

/**
 * Disconnects controller HC. If the VariableCache was enabled with function \nameref{fct:EnableVariableCache} no calls to \nameref{fct:DeleteNode} are necessary.
 *
 * \param HC is the handle to the controller
 *
 * \see \fullref{fct:DetectRemoteController
 * \see \fullref{fct:EnableVariableCache
 * \see \fullref{fct:DeleteNode
 */
EXPORTDLL void DisconnectController(HController HC);

/**
 * Gets an unique handle from the ControllerLib which will not be used internally.
 * \todo{MR: Aha!? ... und wozu brauch ich das dann?}
 *
 * \return
 *     unique handle
 */
EXPORTDLL DWORD GetUniqueHandle(void);

/**
 * Gets the \Qt -RCC file from the controller. 
 * \begin{lstlisting}[caption={GetControllerRCCFile example}]
 * int bytesize;
 * void *data;
 * if ( GetControllerRCCFile(HC, &data, &bytesize) == E_OK ) {
 *   printf("Read %d bytes\n");
 *   hexdump(data,bytesize);
 *   FreeRCCFile(data);
 * }
 * \end{lstlisting}
 * \tip{%
 * \begin{itemize}
 * \item This method is only needed for \Qt -applications which show integrated dialogs.
 * \item The caller of this method is responsible for freeing the allocated memory.
 * \end{itemize}
 * } % tip
 *
 * \param HC is the handle to the controller
 * \param data holds the binary data if the function returns textttE_OK
 * \param bytesize holds the binary data size if the function returns textttE_OK
 *
 * \return
 *     E_OK on success
 *     E_UNIMP if the firmware does not implement this feature
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:FreeRCCFile
 */
EXPORTDLL int GetControllerRCCFile(HController HC, void **data, int *bytesize);

/**
 * Gets the \Qt -help file from the controller. 
 * \begin{lstlisting}[caption={GetControllerQCHFile example}]
 * int bytesize;
 * void *data;
 * if ( GetControllerQCHFile(HC, &data, &bytesize) == E_OK ) {
 *   printf("Read %d bytes\n");
 *   hexdump(data,bytesize);
 *   FreeQCHFile(data);
 * }
 * \end{lstlisting}
 * \tip{The caller of this method is responsible for freeing the allocated memory.}
 *
 * \param HC is the handle to the controller
 * \param data holds the binary data if the function returns textttE_OK
 * \param bytesize holds the binary data size if the function returns textttE_OK
 *
 * \return
 *     E_OK on success
 *     E_UNIMP if the firmware does not implement this feature
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:FreeQCHFile
 */
EXPORTDLL int GetControllerQCHFile(HController HC, void **data, int *bytesize);

/**
 * Gets the \Qt -help file collection from the controller. 
 * \begin{lstlisting}[caption={GetControllerQHCFile example}]
 * int bytesize;
 * void *data;
 * if ( GetControllerQHCFile(HC, &data, &bytesize) == E_OK ) {
 *   printf("Read %d bytes\n");
 *   hexdump(data,bytesize);
 *   FreeQHCFile(data);
 * }
 * \end{lstlisting}
 * \tip{The caller of this method is responsible for freeing the allocated memory.}
 *
 * \param HC is the handle to the controller
 * \param data holds the binary data if the function returns textttE_OK
 * \param bytesize holds the binary data size if the function returns textttE_OK
 *
 * \return
 *     E_OK on success
 *     E_UNIMP if the firmware does not implement this feature
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:FreeQHCFile
 */
EXPORTDLL int GetControllerQHCFile(HController HC, void **data, int *bytesize);

/**
 * Frees the data that was allocated by \nameref{fct:GetControllerRCCFile}.
 * 
 * See \nameref{fct:GetControllerRCCFile} for an example.
 *
 * \param data holds the data obtained by namereffct:GetControllerRCCFile
 *
 * \see \fullref{fct:GetControllerRCCFile
 */
EXPORTDLL void FreeRCCFile(void *data);

/**
 * Frees the data that was allocated by \nameref{fct:GetControllerQCHFile}.
 * 
 * See \nameref{fct:GetControllerQCHFile} for an example.
 *
 * \param data holds the data obtained by namereffct:GetControllerQCHFile
 *
 * \see \fullref{fct:GetControllerQCHFile
 */
EXPORTDLL void FreeQCHFile(void *data);

/**
 * Frees the data that was allocated by \nameref{fct:GetControllerQHCFile}.
 * 
 * See \nameref{fct:GetControllerQHCFile} for an example.
 *
 * \param data holds the data obtained by namereffct:GetControllerQHCFile
 *
 * \see \fullref{fct:GetControllerQHCFile
 */
EXPORTDLL void FreeQHCFile(void *data);

/**
 * Registers a callback for error-events; see \nameref{sec:ErrorCallbackFunction}.
 * \tip{It is possible to have more than one callback for each variable.}
 *
 * \param callback is the callback function
 *
 * \return
 *     E_OK on success
 *     E_EXIST if a callback already exists
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:UnregisterOnError
 * \see \fullref{fct:UnregisterOnErrorSingle
 * \see \fullref{fct:InitControllerLib
 * \see \fullref{sec:ErrorCallbackFunction
 */
EXPORTDLL int RegisterOnError(ErrorCallbackFunction callback);

/**
 * Unregisters all callbacks for error-events before \nameref{fct:DeinitControllerLib}.
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnError
 * \see \fullref{fct:UnregisterOnErrorSingle
 * \see \fullref{fct:DeinitControllerLib
 */
EXPORTDLL int UnregisterOnError(void);

/**
 * Unregisters a callback for error-events before \nameref{fct:DeinitControllerLib}.
 *
 * \param callback is the callback function
 *
 * \return
 *     E_OK on success
 *     E_NOEXIST if the callback was not registered
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnError
 * \see \fullref{fct:UnregisterOnError
 * \see \fullref{fct:DeinitControllerLib
 */
EXPORTDLL int UnregisterOnErrorSingle(ErrorCallbackFunction callback);

/**
 * Gets the current PLC state of the controller. Note that you can register a callback function to get notified if the state changes. This call gets the current value. See \nameref{sec:Supported PLC-States} for supported PLC states.
 * \tip{For performance reasons you should use \nameref{fct:RegisterOnPLCChanged} instead of this function.}
 *
 * \param HC is the handle to the controller
 * \param value holds the PLC-value after returning with textttE_OK
 * \param reserved is reserved for later use
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnPLCChanged
 * \see \fullref{sec:Supported PLC-States
 */
EXPORTDLL int GetPLCState(HController HC, unsigned int *value, unsigned int *reserved);

/**
 * Sends a JobStart to the controller. If a job is selected and the controller is ready for job execution then the controller will start marking. This call returns immediately. 
 * \tip{If this function returns successfully then this does not mean that the job execution has started. Job execution should be watched by using the PLC states.}
 *
 * \param HC is the handle to the controller
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:LoadJob
 * \see \fullref{fct:SelectNode
 * \see \fullref{fct:JobPilot
 * \see \fullref{fct:JobAbort
 * \see \fullref{fct:JobStop
 * \see \fullref{fct:RegisterOnPLCChanged
 */
EXPORTDLL int JobStart(HController HC);

/**
 * Sends a JobPilot (TeachInStart) to the controller.  
 * \tip{If this function returns successfully then this does not mean that the job execution has started. Job execution should be watched by using the PLC states.}
 *
 * \param HC is the handle to the controller
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:JobStart
 * \see \fullref{fct:JobAbort
 * \see \fullref{fct:JobStop
 * \see \fullref{fct:RegisterOnPLCChanged
 */
EXPORTDLL int JobPilot(HController HC);

/**
 * Sends a JobAbort to the controller. If a job is marking then the controller will abort the job execution. This call returns immediately. 
 * \tip{If this function returns successfully then this does not mean that 
 * the job execution has been aborted. Job execution should be watched by using the PLC states.}
 *
 * \param HC is the handle to the controller
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:JobStart
 * \see \fullref{fct:JobPilot
 * \see \fullref{fct:JobStop
 * \see \fullref{fct:RegisterOnPLCChanged
 */
EXPORTDLL int JobAbort(HController HC);

/**
 * Send a JobStop to the controller. If a job is marking then the controller will stop the job execution. This call returns immediately. 
 * \tip{\begin{itemize}\item JobStop only stops the job at a Stop-node in a job. If marking should be aborted immediately then use \nameref{fct:JobAbort} instead.\item When this function returns successfully, this does not mean, that the job execution has been stopped. Job execution should be watched using the PLC states. \end{itemize}}
 *
 * \param HC is the handle to the controller
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:JobStart
 * \see \fullref{fct:JobPilot
 * \see \fullref{fct:JobAbort
 * \see \fullref{fct:RegisterOnPLCChanged
 */
EXPORTDLL int JobStop(HController HC);

/**
 * Executes the script on the controller. The script is executed immediately.
 * \tip{Please refer to the InScript manual \cite{IS3en} for a documentation on the
 * scripting language.}
 * \begin{lstlisting}[caption={ExecuteScript example}]
 * const char *script = "if node_exists("usr.var.x") delete_node("usr.var.x");"
 * ExecuteScript(HC, script);
 * \end{lstlisting}
 *
 * \param HC is the handle to the controller
 * \param script is the script that shall be executed
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 */
EXPORTDLL int ExecuteScript(HController HC, const char *script);

/**
 * Saves the device configuration and the default pen to the controller. If the controller is restarted again then this data will be loaded.
 *
 * \param HC is the handle to the controller
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 */
EXPORTDLL int WriteToNVRAM(HController HC);

/**
 * Sets the path where pens are searched. See \nameref{sec:Loading a Job} for the search strategy.
 *
 * \param HC is the handle to the controller
 * \param path is the path were pens can be found
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetPenPath
 * \see \fullref{fct:SetFontPath
 * \see \fullref{sec:Loading a Job
 */
EXPORTDLL int SetPenPath(HController HC, const char *path);

/**
 * Returns the path where pens are searched. See \nameref{sec:Loading a Job} for the search strategy.
 * \tip{The user has to free the allocated memory using \nameref{fct:CLFree}.}
 * \begin{lstlisting}[caption={GetPenPath example}]
 * char *path = GetPenPath(HC);
 * printf("Penpath: %s", path);
 * CLFree(path);
 * \end{lstlisting}
 *
 * \param HC is the handle to the controller
 *
 * \return
 *     path were pens can be found
 *
 * \see \fullref{fct:SetPenPath
 * \see \fullref{fct:GetFontPath
 * \see \fullref{fct:CLFree
 * \see \fullref{sec:Loading a Job
 */
EXPORTDLL char* GetPenPath(HController HC);

/**
 * Sets the path where TTF- and FDT-fonts are searched. See \nameref{sec:Loading a Job} for the search strategy.
 *
 * \param HC is the handle to the controller
 * \param path is the path were TTF- and FDT-fonts can be found
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:SetPenPath
 * \see \fullref{fct:GetFontPath
 * \see \fullref{sec:Loading a Job
 */
EXPORTDLL int SetFontPath(HController HC, const char *path);

/**
 * Returns the path where TTF- and FDT-fonts are searched. See \nameref{sec:Loading a Job} for the search strategy.
 * \tip{The user has to free the allocated memory using \nameref{fct:CLFree}.}
 * \begin{lstlisting}[caption={GetFontPath example}]
 * const char *path = GetPenPath(HC);
 * printf("Penpath: %s", path);
 * CLFree(path);
 * \end{lstlisting}
 *
 * \param HC is the handle to the controller
 *
 * \return
 *     the path were TTF- and FDT-fonts can be found
 *
 * \see \fullref{fct:SetFontPath
 * \see \fullref{fct:GetPenPath
 * \see \fullref{fct:CLFree
 * \see \fullref{sec:Loading a Job
 */
EXPORTDLL char* GetFontPath(HController HC);

/**
 * Returns a list with all font names which are available for the controller. This includes the fonts on the controller and all TTF- and FDT-fonts that were found
 * in the \texttt{FontPath}.
 *
 * \param HC is the handle to the controller
 *
 * \return
 *     an ARG_FONTINFO-structure with all information about font
 *
 * \see \fullref{fct:DestroyFontInfo
 * \see \fullref{fct:SetFontPath
 */
EXPORTDLL ARG_FONTINFO* GetAllAvailableFontnames(HController HC);

/**
 * Frees the list returned by \nameref{fct:GetAllAvailableFontnames}.
 *
 * \param info is the structure returned by namereffct:GetAllAvailableFontnames
 *
 * \see \fullref{fct:GetAllAvailableFontnames
 */
EXPORTDLL void DestroyFontInfo(ARG_FONTINFO *info);

/**
 * Loads a job file to the controller. It is possible to load jobs that were saved by \nameref{fct:SaveJob} or \nameref{fct:SaveJobXML}.
 * 
 * \tip{\begin{description}\item[Pens] Please note, that extensions of pen files have to be
 * lowercase (\texttt{.pen}).
 * See \nameref{sec:Loading a Job} for details on where the pens and fonts are searched.
 * 
 * \item[Fonts] Only TTF- and FDT-fonts are supported by the library. Please note,
 * that the extension of the font files have to be lowercase (\texttt{.ttf} or \texttt{.fdt}).
 * See \nameref{sec:Loading a Job} for details
 * on where the pens and fonts are searched.
 * 
 * \item[Bitmaps] Upload and streaming of a big set of bitmap formats is
 * supported. The file name of a bitmap is the path as seen from the client
 * application. Note that the bitmap is not searched in the job directory.\end{description}}
 *                       \texttt{0}: leaves the jobs on the controller}
 *                   \texttt{0} does not select this job for execution}
 *
 * \param HC is the handle to the controller
 * \param filename is the file name of the job. The path must be accessible from the client application
 * \param clearfirst texttt1
 * \param select texttt1
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *     
 *     E_NOSPACE if there is not enough memory for the job
 *
 * \see \fullref{sec:Loading a Job
 * \see \fullref{sec:Example Loading a job (Example)
 * \see \fullref{fct:SaveJob
 * \see \fullref{fct:SaveJobXML
 * \see \fullref{fct:GetJobNames
 * \see \fullref{fct:JobClearAll
 * \see \fullref{fct:SelectNode
 * \see \fullref{fct:JobStart
 * \see \fullref{fct:SetPenPath
 * \see \fullref{fct:SetFontPath
 */
EXPORTDLL int LoadJob(HController HC, const char *filename, int clearfirst, int select);

/**
 * Loads a job file to the controller. It is possible to load jobs that were saved by \nameref{fct:SaveJob} or \nameref{fct:SaveJobXML}.
 * \tip{\begin{description}\item[Pens] Please note, that extensions of pen files have to be
 * lowercase (\emph{.pen}).
 * See \nameref{sec:Loading a Job} for details on where the pens and fonts are searched.
 * 
 * \item[Fonts] Only TTF- and FDT-fonts are supported by the library. Please note,
 * that the extension of the font files have to be lowercase (\texttt{.ttf} or \texttt{.fdt}).
 * See \nameref{sec:Loading a Job} for details on where the pens and fonts are searched.
 * 
 * \item[Bitmaps] Upload and streaming of a big set of bitmap formats is
 * supported. The file name of a bitmap is the path as seen from the client
 * application. Note that the bitmap is not searched in the job directory.\end{description}}
 *                       \texttt{0}: leaves the jobs on the controller}
 *                   \texttt{0} does not select this job for execution}
 *
 * \param HC is the handle to the controller
 * \param filename is the file name of the job. The path must be accessible from the client application
 * \param clearfirst texttt1
 * \param select texttt1
 * \param retname is the file name of the job that is returned by the firmware. The path must be accessible from the client application
 * \param retnamelen is the buffer lenght for the job name. The length is usuallynewline 32~bytes
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *     
 *     E_NOSPACE if there is not enough memory for the job
 *
 * \see \fullref{sec:Loading a Job
 * \see \fullref{sec:Example Loading a job (Example)
 * \see \fullref{fct:SaveJob
 * \see \fullref{fct:SaveJobXML
 * \see \fullref{fct:GetJobNames
 * \see \fullref{fct:JobClearAll
 * \see \fullref{fct:SelectNode
 * \see \fullref{fct:JobStart
 * \see \fullref{fct:SetPenPath
 * \see \fullref{fct:SetFontPath
 */
EXPORTDLL int LoadJobExt(HController HC, const char *filename, int clearfirst, int select, char *retname, int retnamelen);

/**
 * Saves a job to the file system.
 *
 * \param HC is the handle to the controller
 * \param jobname is the name of the job, e.g.~textttusr.job.MyJob
 * \param filename is the file name of the job. The path must be accessible from the client application
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *     E_TIMEOUT if the communication with the firmware times out
 *
 * \see \fullref{fct:LoadJob
 * \see \fullref{fct:SaveJobXML
 * \see \fullref{fct:SaveTree
 */
EXPORTDLL int SaveJob(HController HC, const char *jobname, const char *filename);

/**
 * Saves a job in the XML format on the controller.
 * \tip{Not every firmware supports this call. If the firmware supports this feature then the feature list (\nameref{fct:GetFeatureList}) contains
 * the feature \emph{JobXMLFileFormat}.}
 * \todo{MR: "shoud be default" oder "is default"?}
 *
 * \param HC is the handle to the controller
 * \param jobname is the name of the job, e.g. textttusr.job.MyJob
 * \param filename is the file name of the job. The path must be accessible from the client application
 * \param includeDependencies texttt1
 * \param resetModified texttt1
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *     E_UNIMP if the firmware does not implement this feature
 *     E_TIMEOUT if the communication with the firmware times out
 *
 * \see \fullref{fct:LoadJob
 * \see \fullref{fct:SaveTree
 * \see \fullref{fct:GetFeatureList
 */
EXPORTDLL int SaveJobXML(HController HC, const char *jobname, const char *filename, int includeDependencies, int resetModified);

/**
 * Saves a subtree to the file system.
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject from where to save the file
 * \param filename is the file name of the job. The path must be accessible from the client application
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *     E_TIMEOUT if the communication with the firmware times out
 *
 * \see \fullref{fct:LoadTree
 * \see \fullref{fct:SaveTreeAsString
 * \see \fullref{fct:SaveTreeAsXMLString
 * \see \fullref{fct:SaveJob
 */
EXPORTDLL int SaveTree(HController HC, HNodeObject HNO, const char *filename);

/**
 * Saves a subtree to the file system.
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject from where to save the file
 * \param filename is the file name of the job. The path must be accessible from the client application
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *     E_TIMEOUT if the communication with the firmware times out
 *
 * \see \fullref{fct:LoadTree
 * \see \fullref{fct:SaveTreeAsString
 * \see \fullref{fct:SaveTreeAsXMLString
 * \see \fullref{fct:SaveJob
 */
EXPORTDLL int SaveTreeXML(HController HC, HNodeObject HNO, const char *filename);

/**
 * Gets a string representation of a subtree. 
 * \begin{lstlisting}[caption={SaveTreeAsString example}]
 * HNodeObject HNOSrc = GetNode(HC, "usr.job.Job.Transform1");
 * if ( HNOSrc != ARG_INVALID_HANDLE_VALUE ) {
 *   char *tmp = SaveTreeAsString(HC, HNOSrc);
 *   HNodeObject HNODst = GetNode(HC, "usr.job.Job");
 *   if ( HNOSrc != ARG_INVALID_HANDLE_VALUE ) {
 *     char buffer[255];
 *     if ( LoadTreeFromString(HC, HNODst, buffer, 255) == E_OK ) {
 *       printf("The node created is: %s\n", buffer);
 *     }
 *   }
 *   FreeSaveTreeString(tmp);
 * }
 * \end{lstlisting}
 * freeing the memory of that string. \texttt{NULL}, if an error occured or the NodeObject does not exist}
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject from where the string shall be retrieved
 *
 * \return
 *     a string representation of the subtree. The caller of this function is responsible for
 *
 * \see \fullref{fct:FreeSaveTreeString
 * \see \fullref{fct:LoadTreeFromString
 * \see \fullref{fct:SaveTreeAsXMLString
 * \see \fullref{fct:SaveTree
 * \see \fullref{fct:SaveJob
 */
EXPORTDLL char* SaveTreeAsString(HController HC, HNodeObject HNO);

/**
 * Gets the string representation of a subtree. 
 * \begin{lstlisting}[caption={SaveTreeAsXMLString example}]
 * HNodeObject HNOSrc = GetNode(HC, "usr.job.Job.Transform1");
 * if ( HNOSrc != ARG_INVALID_HANDLE_VALUE ) {
 *   char *tmp = SaveTreeAsXMLString(HC, HNOSrc);
 *   HNodeObject HNODst = GetNode(HC, "usr.job.Job");
 *   if ( HNOSrc != ARG_INVALID_HANDLE_VALUE ) {
 *     char buffer[255];
 *     if ( LoadTreeFromString(HC, HNODst, buffer, 255) == E_OK ) {
 *       printf("The node created is: %s\n", buffer);
 *     }
 *   }
 *   FreeSaveTreeString(tmp);
 * }
 * \end{lstlisting}
 * freeing the memory of that string. \texttt{NULL}, if an error occured or the NodeObject does not exist}
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject from where the string shall be retrieved
 *
 * \return
 *     a string representation of the subtree. The caller of this function is responsible for
 *
 * \see \fullref{fct:FreeSaveTreeString
 * \see \fullref{fct:SaveTreeAsString
 * \see \fullref{fct:LoadTreeFromString
 * \see \fullref{fct:SaveTree
 * \see \fullref{fct:SaveJob
 */
EXPORTDLL char* SaveTreeAsXMLString(HController HC, HNodeObject HNO);

/**
 * Frees the string that was retrieved by a call to \nameref{fct:SaveTreeAsString} or\newline 
 * \nameref{fct:SaveTreeAsXMLString}.
 * \tip{This function only has to be used on \Windows . On \Linux\ and \macOS\ the application code
 * can free memory, which was alloced to a shared library.}
 * \begin{lstlisting}[caption={FreeSaveTreeString example}]
 * HNodeObject HNOSrc = GetNode(HC, "usr.job.Job.Transform1");
 * if ( HNOSrc != ARG_INVALID_HANDLE_VALUE ) {
 *   char *tmp = SaveTreeAsXMLString(HC, HNOSrc);
 *   HNodeObject HNODst = GetNode(HC, "usr.job.Job");
 *   if ( HNOSrc != ARG_INVALID_HANDLE_VALUE ) {
 *     char buffer[255];
 *     if ( LoadTreeFromString(HC, HNODst, buffer, 255) == E_OK ) {
 *       printf("The node created is: %s\n", buffer);
 *     }
 *   }
 *   FreeSaveTreeString(tmp);
 * }
 * \end{lstlisting}
 *
 * \param string is the string obtained by namereffct:SaveTreeAsString
 *
 * \see \fullref{fct:SaveTreeAsString
 * \see \fullref{fct:SaveTreeAsXMLString
 */
EXPORTDLL void FreeSaveTreeString(char *string);

/**
 * Loads a file, that contains variable information, to the controller. Such files usually are \texttt{*.tre} files.
 * 
 * It is possible to load files that were saved by \nameref{fct:SaveTree} or \nameref{fct:SaveTreeXML}.
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param filename full path to the texttt*.tre
 *
 * \return
 *     E_OK on success
 *     E_NFILE if the file was not found or could not be read
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:SaveTreeAsString
 * \see \fullref{fct:SaveTreeAsXMLString
 */
EXPORTDLL int LoadTree(HController HC, HNodeObject HNO, const char *filename);

/**
 * Loads the string representation of a subtree to the controller. See \nameref{fct:SaveTreeAsString} and \nameref{fct:SaveTreeAsXMLString}
 * for an example.
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to a NodeObject
 * \param str is the string containing the subtree
 * \param retname is the buffer for the name of the node that was created by thenewline firmware
 * \param retnamelen is the size of the buffer. If the buffer is too small then no name will be returned.
 *
 * \return
 *     E_OK on success
 *     E_NFILE if the file was not found or could not be read
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:SaveTreeAsString
 * \see \fullref{fct:SaveTreeAsXMLString
 */
EXPORTDLL int LoadTreeFromString(HController HC, HNodeObject HNO, const char *str, char *retname, int retnamelen);

/**
 * Loads the string representation of a subtree to the controller. See \nameref{fct:SaveTreeAsString} and \nameref{fct:SaveTreeAsXMLString}
 * for an example.
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param str is the string containing the subtree
 * \param index is the index where to put the subtree
 * \param retname is the buffer for the name of the node that was created by thenewline firmware
 * \param retnamelen is the size of the buffer. If the buffer is too small then no name will be returned
 *
 * \return
 *     E_OK on success
 *     E_NFILE if the file was not found or could not be read
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:SaveTreeAsString
 * \see \fullref{fct:SaveTreeAsXMLString
 */
EXPORTDLL int LoadTreeFromStringExt(HController HC, HNodeObject HNO, const char *str, int index, char *retname, int retnamelen);

/**
 * Saves a pen to the file system.
 *
 * \param HC is the handle to the controller
 * \param jobname is the name of the pen, e.g.~textttusr.pens.MyPen
 * \param filename is the file name of the pen. The path must be accessible from the client application
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *     E_TIMEOUT if the communication with the firmware times out
 *
 * \see \fullref{fct:LoadPen
 * \see \fullref{fct:SaveTree
 */
EXPORTDLL int SavePen(HController HC, const char *penname, const char *filename);

/**
 * Loads the pen with a given file name to the controller. If the pen name already exists there then the pen on the controller is completly
 * replaced with the new one. 
 * 
 * If a pen is uploaded, while a job is running, then \texttt{E_BUSY} 
 * is returned. If a job is selected then the PLC state \texttt{JOB_READY} goes away while 
 * loading the pen. 
 * \tip{\begin{itemize}\item \emph{default} and \emph{bitmap} are
 * reserved for internal use. If these names are used as penname the
 * result on the controller is undefined. For piloting a pen named \emph{pilot} 
 * has to loaded to the controller
 * \item The upload of a pen is only allowed when no job is running
 * \item The penpath, set with \nameref{fct:SetPenPath}, is not used for this
 * function \end{itemize}}
 *
 * \param HC is the handle to the controller
 * \param penname is the name for the pen on the controller
 * \param filename is the file name of the pen. The path has to be accessible from the client application
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *     E_BUSY if a job is running on the controller while the pen is being uploaded
 *     E_TIMEOUT if the communication with the firmware times out
 *     E_NOSPACE if there is not enough memory for the pen
 *
 * \see \fullref{fct:LoadJob
 * \see \fullref{fct:LoadPen
 * \see \fullref{fct:RegisterOnPLCChanged
 */
EXPORTDLL int LoadPen(HController HC, const char *penname, const char *filename);

/**
 * Loads a TTF- or FDT-font to the controller.
 *
 * \param HC is the handle to the controller
 * \param filename is the file name of the TTF- or FDT-font. The path has to be accessible from the client application
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *     E_TIMEOUT if the communication with the firmware times out
 *     E_NOEXIST if the file does not exist
 *
 * \see \fullref{fct:LoadJob
 * \see \fullref{fct:LoadPen
 * \see \fullref{fct:SetPenPath
 * \see \fullref{fct:RegisterOnPLCChanged
 */
EXPORTDLL int LoadFont(HController HC, const char *filename);

/**
 * Loads XML-data to the controller. Only available if the firmware supports the feature \emph{XMLDataLoad}.
 *
 * \param HC is the handle to the controller
 * \param filename is the file name which holds the XML-data
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *     E_TIMEOUT if the communication with the firmware times out
 *     E_NFILE if the file does not exist
 *     E_UNIMP if the firmware does not support this feature
 *
 * \see \fullref{fct:LoadDataXML
 */
EXPORTDLL int LoadDataXMLFromFile(HController HC, const char *filename);

/**
 * Loads XML-data to the controller. Only available if the firmware supports the feature \emph{XMLDataLoad}.
 *
 * \param HC is the handle to the controller
 * \param xmldata is the the XML-data
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *     E_TIMEOUT if the communication with the firmware times out
 *     E_UNIMP if the firmware does not support this feature
 *
 * \see \fullref{fct:LoadDataXMLFromFile
 */
EXPORTDLL int LoadDataXML(HController HC, const char *xmldata);

/**
 * Gets a node for the given variable name, e.g.~\texttt{usr.job.Job}). If the variable cache was enabled with
 * \nameref{fct:EnableVariableCache} the handle to the variable is 
 * returned. If the variable cache was not enabled this function only
 * creates a virtual \emph{NodeObject}. It is not filled with any data
 * until a call to \nameref{fct:ReadNode} is executed. If the ID 
 * (\nameref{fct:GetNodeID}) of the node is -1 the node does not exist
 * on the controller. 
 * 
 * If the variable cache was enabled the root-node can be get with
 * \begin{lstlisting}
 * HNodeObject HRootNode = GetNode(HC, "");
 * \end{lstlisting}
 *
 * \param HC is the handle to the controller
 * \param name is the name of the controller variable
 *
 * \return
 *     Handle to a NodeObject
 *     ARG_INVALID_HANDLE_VALUE on error
 *
 * \see \fullref{fct:GetNodeFromCache
 * \see \fullref{fct:ReadNode
 * \see \fullref{fct:WriteNode
 * \see \fullref{fct:EnableVariableCache
 * \see \fullref{fct:GetNodeID
 * \see \fullref{fct:GetNodeInfo
 */
EXPORTDLL HNodeObject GetNode(HController HC, const char* name);

/**
 * If the variable cache was enabled with \nameref{fct:EnableVariableCache} this
 * method returns a handle to this node otherwise it returns
 * \texttt{ARG_INVALID_HANDLE_VALUE}. 
 * The difference to \nameref{fct:GetNode} is, that \nameref{fct:GetNode}
 * returns a valid handle even if the node does not exist on the controller
 * because if the variable cache was not enabled a call to
 * \nameref{fct:ReadNode} is necessary to find out, if the node exists.
 * \begin{lstlisting}[caption={GetNodeFromCache example}]
 * HNodeObject HNO = GetNodeFromCache(HC, "usr.var.x");
 * if ( HNO != ARG_INVALID_HANDLE_VALUE ) {
 *   // Node usr.var.x exists.
 * }
 * \end{lstlisting}
 *
 * \param HC is the handle to the controller
 * \param name is the name of the controller variable
 *
 * \return
 *     Handle to a NodeObject
 *     ARG_INVALID_HANDLE_VALUE if the node dos not exist
 *
 * \see \fullref{fct:GetNode
 * \see \fullref{fct:EnableVariableCache
 * \see \fullref{fct:ReadNode
 */
EXPORTDLL HNodeObject GetNodeFromCache(HController HC, const char *name);

/**
 * Gets information about a NodeObject. See \nameref{sec:NodeInfo}
 * for a detailed description of the ARG_NODEINFO-structure.
 * \begin{lstlisting}[caption={GetNodeInfo example}]
 * HNodeObject HNO = GetNode(HC, "stat.time.TimeStr");
 * if ( HNO != ARG_INVALID_HANDLE_VALUE ) {
 *   ARG_NODEINFO *info = GetNodeInfo(HC, HNO);
 *   if ( info != NULL ) {
 *     printf("Nodename:  %s\n", info->fullname);
 *     printf("Value:     %s\n", info->value);
 *     printf("Unit:      %s\n", info->unit);
 *     DestroyNodeInfo(info);
 *   }
 * }
 * \end{lstlisting}
 * \tip{The decimal separator is a dot by default. To change this to your current locale call 
 * \nameref{fct:DisableFloatingPointToStringDot}.}
 *
 * \param HC is the handle to the controller
 * \param HC is the handle to the NodeObject
 *
 * \return
 *     
 *
 * \see \fullref{sec:NodeInfo
 * \see \fullref{fct:DisableFloatingPointToStringDot
 * \see \fullref{fct:GetNodeInfoExt
 * \see \fullref{fct:DestroyNodeInfo
 * \see \fullref{fct:GetNode
 */
EXPORTDLL ARG_NODEINFO* GetNodeInfo(HController HC, HNodeObject HNO);

/**
 * Gets information about a NodeObject. See \nameref{sec:NodeInfo}
 * for a detailed description of the ARG_NODEINFO-structure.
 * 
 * This function performs better than \nameref{fct:GetNodeInfo} in cases when only some informations of
 * the node are needed. Only the \enquote{expensive} string-copies can be influenced with this function.
 * The following flags can be passed to this function. 
 * \begin{description}
 *    \item[NODEINFO_FULLNAME]
 *      If this flag is set, the full name will be set in the \newline \nameref{sec:NodeInfo}-structure.
 *    \item[NODEINFO_NAME]
 *      If this flag is set, the name will be set in the \newline \nameref{sec:NodeInfo}-structure.
 *    \item[NODEINFO_PATH]
 *      If this flag is set, the path will be set in the \nameref{sec:NodeInfo}-structure.
 *    \item[NODEINFO_VALUE]
 *      If this flag is set, the value will be set in the \newline \nameref{sec:NodeInfo}-structure.
 *    \item[NODEINFO_PRIV]
 *      If this flag is set, the priv will be set in the \nameref{sec:NodeInfo}-structure.
 *    \item[NODEINFO_UNIT]
 *      If this flag is set, the unit will be set in the \nameref{sec:NodeInfo}-structure.
 *    \item[NODEINFO_TYPESTRING]
 *      If this flag is set, the typestring will be set in the \nameref{sec:NodeInfo}-structure.
 *    \item[NODEINFO_ALL]
 *      If this flag is set, all fields will be set.
 * \end{description}
 * \begin{lstlisting}[caption={GetNodeInfoExt example}]
 * HNodeObject HNO = GetNode(HC, "stat.time.TimeStr");
 * if ( HNO != ARG_INVALID_HANDLE_VALUE ) {
 *   ARG_NODEINFO *info = GetNodeInfoExt(HC, HNO, NODEINFO_FULLNAME | NODEINFO_VALUE);
 *   if ( info != NULL ) {
 *     printf("Nodename:  %s\n", info->fullname);
 *     printf("Value:     %s\n", info->value);
 *     DestroyNodeInfo(info);
 *   }
 * }
 * \end{lstlisting}
 * \tip{The decimal separator is a period (dot) by default. To change this to your current locale call 
 * \nameref{fct:DisableFloatingPointToStringDot}.}
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 *
 * \return
 *     the pointer to a filled ARG_NODEINFO-structure or textttNULL
 *
 * \see \fullref{sec:NodeInfo
 * \see \fullref{fct:DisableFloatingPointToStringDot
 * \see \fullref{fct:GetNodeInfo
 * \see \fullref{fct:DestroyNodeInfo
 * \see \fullref{fct:GetNode
 */
EXPORTDLL ARG_NODEINFO* GetNodeInfoExt(HController HC, HNodeObject HNO, unsigned int flags);

/**
 * Destroys a NodeInfo which was allocated with \nameref{fct:GetNodeInfo}.
 *
 * \param info is the NodeInfo which was allocated with namereffct:GetNodeInfo
 *
 * \see \fullref{fct:GetNodeInfo
 * \see \fullref{fct:GetNodeInfoExt
 */
EXPORTDLL void DestroyNodeInfo(ARG_NODEINFO *info);

/**
 * Reads the current value of a controller variable. To get notified
 * when a variable changes on the controller use \nameref{fct:RegisterOnValueChanged}.
 * \tip{Calls to this function are only needed when the variable cache
 * was not enabled with \nameref{fct:EnableVariableCache}.}
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 *
 * \return
 *     E_OK on success
 *     E_TIMEOUT if the communication with the firmware times out
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetNode
 * \see \fullref{fct:WriteNode
 * \see \fullref{fct:RegisterOnValueChanged
 * \see \fullref{fct:EnableVariableCache
 * \see \fullref{fct:GetNodeInfo
 * \see \fullref{fct:GetNodeInfoExt
 */
EXPORTDLL int ReadNode(HController HC, HNodeObject HNO);

/**
 * Writes the current value of a NodeObject to an associated controller variable.
 * If changing the node forces other nodes to change then
 * \nameref{fct:WriteNodeSync} should be used.
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 *
 * \return
 *     E_OK on success
 *     E_TIMEOUT if the communication with the firmware times out
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetNode
 * \see \fullref{fct:ReadNode
 * \see \fullref{fct:WriteNodeSync
 */
EXPORTDLL int WriteNode(HController HC, HNodeObject HNO);

/**
 * Writes the current value of a NodeObject to an associated controller variable.
 * This function waits until the firmware has sent all updates. This is useful if
 * setting a variable changes other variables.
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 *
 * \return
 *     E_OK on success
 *     E_TIMEOUT if the communication with the firmware times out
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetNode
 * \see \fullref{fct:ReadNode
 */
EXPORTDLL int WriteNodeSync(HController HC, HNodeObject HNO);

/**
 * Writes the current value of a NodeObject to an associated controller variable.
 * If changing the node forces other nodes to change then
 * \nameref{fct:WriteNodeSync} should be used.
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 *
 * \return
 *     E_OK on success
 *     E_TIMEOUT if the communication with the firmware times out
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetNode
 * \see \fullref{fct:ReadNode
 * \see \fullref{fct:WriteNodeSync
 */
EXPORTDLL int WriteNodeAndStoreLocal(HController HC, HNodeObject HNO);

/**
 * Reads the flags of the given NodeObject. The flags are normally used for
 * debugging issues only.
 * \tip{With \nameref{fct:GetNodeInfo} it is possible to get all information
 * about a node with a single call.}
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param flags holds the flags after returning with textttE_OK
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetNode
 * \see \fullref{fct:RegisterOnValueChanged
 * \see \fullref{fct:EnableVariableCache
 * \see \fullref{fct:GetNodeInfo
 * \see \fullref{fct:GetNodeInfoExt
 * \see \fullref{fct:WriteNode
 * \see \fullref{fct:SetNodeFlags
 * \see \fullref{fct:RegisterOnFlagsChanged
 * \see \fullref{fct:CreateAllowed
 * \see \fullref{fct:DeleteAllowed
 * \see \fullref{fct:ExtendRangeAllowed
 * \see \fullref{fct:FlagsModifyAllowed
 * \see \fullref{fct:ModifyAllowed
 * \see \fullref{fct:RenameAllowed
 * \see \fullref{fct:IsConsumable
 * \see \fullref{fct:IsControlledByDevice
 * \see \fullref{fct:IsForcePen
 * \see \fullref{fct:IsMirrored
 * \see \fullref{fct:IsModified
 * \see \fullref{fct:IsOwnedByDevice
 * \see \fullref{fct:IsPenable
 * \see \fullref{fct:IsProtected
 * \see \fullref{fct:IsQuicksaveable
 * \see \fullref{fct:IsUserdefined
 * \see \fullref{fct:IsWriteable
 * \see \fullref{fct:SetConsumable
 * \see \fullref{fct:SetControlledByDevice
 * \see \fullref{fct:SetForcePen
 * \see \fullref{fct:SetMirrored
 * \see \fullref{fct:SetQuicksaveable
 */
EXPORTDLL int GetNodeFlags(HController HC, HNodeObject HNO, unsigned int *flags);

/**
 * Sets and unsets specific flags for the node. Please note, this function
 * returns immediately. If flags are actually changed then the
 * \nameref{fct:FlagsChangeCallbackFunction} is called.
 * \tip{Please use this function with special care. Normally there is no need
 * for this function.}
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param setflags holds the flags that should be set to 1
 * \param unsetflags holds the flags that should be set to 0
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:FlagsChangeCallbackFunction
 * \see \fullref{fct:FlagsChangeCallbackFunctionExt
 * \see \fullref{fct:GetNodeFlags
 * \see \fullref{fct:CreateAllowed
 * \see \fullref{fct:DeleteAllowed
 * \see \fullref{fct:ExtendRangeAllowed
 * \see \fullref{fct:FlagsModifyAllowed
 * \see \fullref{fct:ModifyAllowed
 * \see \fullref{fct:RenameAllowed
 * \see \fullref{fct:IsConsumable
 * \see \fullref{fct:IsControlledByDevice
 * \see \fullref{fct:IsForcePen
 * \see \fullref{fct:IsMirrored
 * \see \fullref{fct:IsModified
 * \see \fullref{fct:IsOwnedByDevice
 * \see \fullref{fct:IsPenable
 * \see \fullref{fct:IsProtected
 * \see \fullref{fct:IsQuicksaveable
 * \see \fullref{fct:IsUserdefined
 * \see \fullref{fct:IsWriteable
 * \see \fullref{fct:SetConsumable
 * \see \fullref{fct:SetControlledByDevice
 * \see \fullref{fct:SetForcePen
 * \see \fullref{fct:SetMirrored
 * \see \fullref{fct:SetQuicksaveable
 */
EXPORTDLL int SetNodeFlags(HController HC, HNodeObject HNO, unsigned int setflags, unsigned int unsetflags);

/**
 * Sets the minimum and maximum value of a user created variable. If the variable should
 * have no minimum and maximum value then vmax has to be smaller than vmin.
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param vmin is the new minimum value of the variable
 * \param vmax is the new maximum value of the variable
 *
 * \return
 *     E_OK on success
 *     E_UNIMPL if the firmware does not support setting
 *     E_NALLOWED if setting the min/max value of the NodeObject is not allowed
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:CreateAllowed
 * \see \fullref{fct:DeleteAllowed
 * \see \fullref{fct:ExtendRangeAllowed
 * \see \fullref{fct:FlagsModifyAllowed
 * \see \fullref{fct:ModifyAllowed
 * \see \fullref{fct:RenameAllowed
 * \see \fullref{fct:IsConsumable
 * \see \fullref{fct:IsControlledByDevice
 * \see \fullref{fct:IsForcePen
 * \see \fullref{fct:IsMirrored
 * \see \fullref{fct:IsModified
 * \see \fullref{fct:IsOwnedByDevice
 * \see \fullref{fct:IsPenable
 * \see \fullref{fct:IsProtected
 * \see \fullref{fct:IsQuicksaveable
 * \see \fullref{fct:IsUserdefined
 * \see \fullref{fct:IsWriteable
 * \see \fullref{fct:SetConsumable
 * \see \fullref{fct:SetControlledByDevice
 * \see \fullref{fct:SetForcePen
 * \see \fullref{fct:SetMirrored
 * \see \fullref{fct:SetQuicksaveable
 */
EXPORTDLL int SetNodeMinMax(HController HC, HNodeObject HNO, float vmin, float vmax);

/**
 * Sets the unit of a user created variable.
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param unit is the new unit of the variable
 *
 * \return
 *     E_OK on success
 *     E_UNIMPL if the firmware does not support setting
 *     E_NALLOWED if setting the unit of the NodeObject is not allowed
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:CreateAllowed
 * \see \fullref{fct:DeleteAllowed
 * \see \fullref{fct:ExtendRangeAllowed
 * \see \fullref{fct:FlagsModifyAllowed
 * \see \fullref{fct:ModifyAllowed
 * \see \fullref{fct:RenameAllowed
 * \see \fullref{fct:IsConsumable
 * \see \fullref{fct:IsControlledByDevice
 * \see \fullref{fct:IsForcePen
 * \see \fullref{fct:IsMirrored
 * \see \fullref{fct:IsModified
 * \see \fullref{fct:IsOwnedByDevice
 * \see \fullref{fct:IsPenable
 * \see \fullref{fct:IsProtected
 * \see \fullref{fct:IsQuicksaveable
 * \see \fullref{fct:IsUserdefined
 * \see \fullref{fct:IsWriteable
 * \see \fullref{fct:SetConsumable
 * \see \fullref{fct:SetControlledByDevice
 * \see \fullref{fct:SetForcePen
 * \see \fullref{fct:SetMirrored
 * \see \fullref{fct:SetQuicksaveable
 */
EXPORTDLL int SetNodeUnit(HController HC, HNodeObject HNO, const char *unit);

/**
 * Gets the current state of the state machine in the NodeObject.
 * For supported states see \nameref{sec:Supported NodeObject-States}.
 *
 * \param HC is the handle to the controller
 * \param HNO is handle to the NodeObject
 *
 * \return
 *     the state of the NodeObject
 *
 * \see \fullref{sec:Supported NodeObject-States
 * \see \fullref{fct:NodeStateChangeCallbackFunction
 */
EXPORTDLL int GetNodeState(HController HC, HNodeObject HNO);

/**
 * Gets the parent of a node if this parent is also loaded by the
 * ControllerLib. 
 * \tip{Only works correctly if the VariableCache was enabled with
 * \nameref{fct:EnableVariableCache}.}
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 *
 * \return
 *     the handle to the parent node
 *     ARG_INVALID_HANDLE_VALUE if not loaded by the library
 *
 * \see \fullref{fct:EnableVariableCache
 * \see \fullref{fct:GetSubnodes
 * \see \fullref{fct:GetNode
 */
EXPORTDLL HNodeObject GetParentNode(HController HC, HNodeObject HNO);

/**
 * Gets the number of children of a node.
 * \tip{Only works correctly when the VariableCache was enabled with
 * \nameref{fct:EnableVariableCache}.}
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 *
 * \return
 *     the number of subnodes of a NodeObject
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:EnableVariableCache
 * \see \fullref{fct:GetSubnodes
 * \see \fullref{fct:GetNode
 */
EXPORTDLL int GetSubnodesCount(HController HC, HNodeObject HNO);

/**
 * Selects a node for job execution on the controller.
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 *
 * \return
 *     E_OK on success
 *     E_TIMEOUT if the communication with the firmware times out
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:SelectNodeByName
 * \see \fullref{fct:DeselectNode
 * \see \fullref{fct:LoadJob
 * \see \fullref{fct:JobStart
 * \see \fullref{fct:RegisterOnPLCChanged
 */
EXPORTDLL int SelectNode(HController HC, HNodeObject HNO);

/**
 * Selects a node for job execution on the controller by path name.
 *
 * \param HC is the handle to the controller
 * \param varname is the complete path to the node to be selected
 *
 * \return
 *     E_OK on success
 *     E_TIMEOUT if the communication with the firmware times out
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:SelectNode
 * \see \fullref{fct:DeselectNodeByName
 * \see \fullref{fct:LoadJob
 * \see \fullref{fct:JobStart
 * \see \fullref{fct:RegisterOnPLCChanged
 */
EXPORTDLL int SelectNodeByName(HController HC, const char *varname);

/**
 * Deselects the given NodeObject on the controller.
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 *
 * \return
 *     E_OK on success
 *     E_TIMEOUT if the communication with the firmware times out
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:DeselectNodeByName
 * \see \fullref{fct:SelectNode
 * \see \fullref{fct:LoadJob
 * \see \fullref{fct:JobStart
 * \see \fullref{fct:RegisterOnPLCChanged
 */
EXPORTDLL int DeselectNode(HController HC, HNodeObject HNO);

/**
 * Deselects a node on the controller by its full qualified name.
 *
 * \param HC is the handle to the controller
 * \param varname is the fully qualified name of the node to be deselected
 *
 * \return
 *     E_OK on success
 *     E_TIMEOUT if the communication with the firmware times out
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:DeselectNode
 * \see \fullref{fct:SelectNode
 * \see \fullref{fct:LoadJob
 * \see \fullref{fct:JobStart
 * \see \fullref{fct:RegisterOnPLCChanged
 */
EXPORTDLL int DeselectNodeByName(HController HC, const char *varname);

/**
 * Sets the node to be marked in teach-in mode to the given NodeObject. To find out
 * which node is the current TeachInNode, the \texttt{VAR:STRING} variable \newline
 * \texttt{dev.sas.TeachIn.CurrentTeachInNode} holds the name. 
 * 
 * \tip{For teach-in to work properly, a job on the controller must be
 * selected.  The PLC states \emph{JOB_READY} and \emph{DEVICES_READY} have
 * to be present and a pen named \emph{pilot} has to be on the controller.}
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 *
 * \return
 *     E_OK on success
 *     E_TIMEOUT if the communication with the firmware times out
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:TeachInSetCurrentByName
 * \see \fullref{fct:JobPilot
 */
EXPORTDLL int TeachInSetCurrent(HController HC, HNodeObject HNO);

/**
 * Sets the node to be marked in teach-in mode to the given NodeObject by path name. To find out
 * which node is the current TeachInNode the \texttt{VAR:STRING} variable  \newline
 * \texttt{dev.sas.TeachIn.CurrentTeachInNode} can be read.
 * 
 * \tip{For teach-in to work properly, a job on the controller must be
 * selected. The PLC states \emph{JOB_READY} and \emph{DEVICES_READY} have
 * to be present and a pen name \emph{pilot} has to be on the controller.}
 *
 * \param HC is the handle to the controller
 * \param varname is the complete name of the variable to be marked in teach-in mode
 *
 * \return
 *     E_OK on success
 *     E_TIMEOUT if the communication with the firmware times out
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:TeachInSetCurrent
 * \see \fullref{fct:JobPilot
 */
EXPORTDLL int TeachInSetCurrentByName(HController HC, const char *varname);

/**
 * Deletes the \emph{connection} to a variable on the controller. This
 * call \emph{does not delete} the variable from the controller and is only
 * necessary if the variable cache was not enabled with
 * \nameref{fct:EnableVariableCache}.
 * \tip{To delete a variable on the controller use \nameref{fct:DeleteNodeOnController}
 * or \nameref{fct:DeleteNodeOnControllerByName}.}
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 *
 * \return
 *     E_OK on success
 *     E_NALLOWED if the variable cache was enabled
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetNode
 * \see \fullref{fct:EnableVariableCache
 * \see \fullref{fct:DeleteNodeOnController
 * \see \fullref{fct:DeleteNodeOnControllerByName
 * \see \fullref{fct:DeleteSingleNodeOnController
 * \see \fullref{fct:DeleteSingleNodeOnControllerByName
 */
EXPORTDLL int DeleteNode(HController HC, HNodeObject HNO);

/**
 * Deletes a node including its subnodes from the controller. The node has to be identified by its handle. If the call was successfull then
 * \texttt{E_OK} is returned. If the node did not exist on the controller,
 * the call returns also with \texttt{E_OK}, because the call itself was
 * successfull, but a SysMessage will be thrown by the controller.
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:DeleteNodeOnControllerByName
 * \see \fullref{fct:DeleteSingleNodeOnControllerByName
 * \see \fullref{fct:DeleteSingleNodeOnController
 * \see \fullref{fct:CreateNodeOnController
 * \see \fullref{fct:DeleteAllowed
 */
EXPORTDLL int DeleteNodeOnController(HController HC, HNodeObject HNO);

/**
 * Deletes a node from the controller. The node has to be identified by its name. If the call was successfull then
 * \texttt{E_OK} is returned. If the node did not exist on the controller then
 * the call returns also with \texttt{E_OK}, because the call itself was
 * successfull, but a SysMessage will be thrown by the controller.
 * \tip{The node including all subnodes is deleted on the controller.}
 *
 * \param HC is the handle to the controller
 * \param nodename is the full name of the variable; e.g.~textttusr.var.myvar
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:DeleteNodeOnController
 * \see \fullref{fct:DeleteSingleNodeOnControllerByName
 * \see \fullref{fct:DeleteSingleNodeOnController
 * \see \fullref{fct:CreateNodeOnController
 * \see \fullref{fct:DeleteAllowed
 */
EXPORTDLL int DeleteNodeOnControllerByName(HController HC, const char *nodename);

/**
 * \notice{This function is deprecated.}{\vspace{-1.5ex}}{\item[\textbullet] Use \nameref{fct:DeleteNodeOnController} or \newline \nameref{fct:DeleteNodeOnControllerByName} instead.}
 * The description of this function has intentionally been omitted.
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 *
 * \return
 *     E_NOEXIST
 *
 * \see \fullref{fct:DeleteSingleNodeOnControllerByName
 * \see \fullref{fct:DeleteNodeOnControllerByName
 * \see \fullref{fct:DeleteNodeOnController
 * \see \fullref{fct:CreateNodeOnController
 * \see \fullref{fct:DeleteAllowed
 */
EXPORTDLL int DeleteSingleNodeOnController(HController HC, HNodeObject HNO);

/**
 * \notice{This function is deprecated.}{\vspace{-1.5ex}}{\item[\textbullet] \flushleft Use \nameref{fct:DeleteNodeOnController} or \nameref{fct:DeleteNodeOnControllerByName} instead.}
 * The description of this function has intentionally been omitted.
 *
 * \param HC is the handle to the controller
 * \param nodename is the full name of the variable; e.g. textttusr.var.myvar
 *
 * \return
 *     E_NOEXIST
 *
 * \see \fullref{fct:DeleteSingleNodeOnController
 * \see \fullref{fct:DeleteNodeOnControllerByName
 * \see \fullref{fct:DeleteNodeOnController
 * \see \fullref{fct:CreateNodeOnController
 * \see \fullref{fct:DeleteAllowed
 */
EXPORTDLL int DeleteSingleNodeOnControllerByName(HController HC, const char *nodename);

/**
 * Renames a node on the controller. This function returns immediately. When
 * the name changed on the controller then this can be followed with a \newline
 * \nameref{fct:NameChangeCallbackFunction}.
 * \tip{\begin{itemize}\item This only changes the last name of the variable.
 * \item To move a node to another subtree use \nameref{fct:MoveSubtree}.\end{itemize}}
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param newname the new last name of the node
 *
 * \return
 *     E_OK on success
 *     E_NALLOWED if renaming is not allowed
 *     E_TIMEOUT if the communication with the firmware times out
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnNameChanged
 * \see \fullref{fct:MoveSubtree
 */
EXPORTDLL int RenameNode(HController HC, HNodeObject HNO, const char *newname);

/**
 * Moves a subtree to the target NodeObject with the given index. This
 * function returns immediately. When the node is moved on the controller
 * this can be followed with a \nameref{fct:NodeMovedCallbackFunction}.
 * \tip{\begin{itemize}\item Even when this function returns with \texttt{E_OK} it is possible that
 * moving the node fails.
 * \item To rename a node only use \nameref{fct:RenameNode}.\end{itemize}}
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject to be moved
 * \param target is the handle to the target NodeObject
 * \param index is the new index of the node
 *
 * \return
 *     E_OK on success
 *     E_TIMEOUT if the communication with the firmware times out
 *     E_NALLOWED if moving is not allowed
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnNodeMoved
 * \see \fullref{fct:RenameNode
 */
EXPORTDLL int MoveSubtree(HController HC, HNodeObject HNO, HNodeObject target, int index);

/**
 * Creates a node on the controller. When this function returns the node on
 * the controller is already created.
 * \tip{It may not be allowed to create a node on most of the subtrees.
 * Normally it is only advisable to do this in \texttt{usr.var}.}
 *
 * \param HC is the handle to the controller
 * \param nodename is the full name of a variable, e.g.~textttusr.var.myvar
 * \param nodetype is the type of the new variable; see namereffct:GetNodeType
 * \param index is the index in the tree. Only in special cases,,like job trees,,this is other than~texttt0
 *
 * \return
 *     E_OK on success
 *     E_TIMEOUT if the communication with the firmware times out
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:CreateJobNodeOnController
 * \see \fullref{fct:CreateAllowed
 * \see \fullref{fct:DeleteNodeOnControllerByName
 * \see \fullref{fct:GetNodeType
 * \see \fullref{fct:CreateNodeOnControllerAsync
 */
EXPORTDLL int CreateNodeOnController(HController HC, const char *nodename, VAR_TYPE vartype, int index);

/**
 * Creates a node on the controller. 
 * When this function returns the node on the controller is already created and \texttt{retname} is 
 * filled with the name that the controller returned.
 * \tip{It may not be allowed to create a node on most of the subtrees.
 * Normally it is only advisable to do this in \texttt{usr.var}.}
 * \texttt{E_NOSPACE} is returned.}
 *
 * \param HC is the handle to the controller
 * \param nodename is the full name of the variable, e.g.~textttusr.var.myvar
 * \param vartype is the type of the new variable; see namereffct:GetNodeType
 * \param index is the index in the tree. Only in special cases,,like job trees,,this is other than~texttt0
 * \param retname is the place where the ControllerLib can store the name that the firmware returned
 * \param retnamelen is the length of the buffer for retname. This must be at least 32~characters. If it is smaller then
 *
 * \return
 *     E_OK on success
 *     E_NOSPACE if the buffer for retname is too small
 *     E_TIMEOUT if the communication with the firmware times out
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:CreateJobNodeOnController
 * \see \fullref{fct:CreateAllowed
 * \see \fullref{fct:DeleteNodeOnControllerByName
 * \see \fullref{fct:GetNodeType
 * \see \fullref{fct:CreateNodeOnControllerAsync
 */
EXPORTDLL int CreateNodeOnControllerExt(HController HC, const char *nodename, VAR_TYPE vartype, int index, char *retname, int retnamelen);

/**
 * Creates a node on the controller. This call returns immediately and does
 * not wait for notification from the controller. When the node on the
 * controller is actually created this can be catched in a
 * \nameref{fct:NodeCreatedCallbackFunction} or in a 
 * \nameref{fct:NodeCreatedCallbackFunctionExt}. If a lot of nodes have to be
 * created this function should be used because it is faster than
 * \nameref{fct:CreateNodeOnController}.
 * \tip{It may not be allowed to create a node on most of the subtrees.
 * Normally it is only advisable to do this in \texttt{usr.var}.}
 *
 * \param HC is the handle to the controller
 * \param nodename is the full name of a variable, e.g. textttusr.var.myvar
 * \param vartype is the type of the new variable; see namereffct:GetNodeType
 * \param index is the index in the tree. Only in special cases,,like job trees,,this is other than~texttt0
 *
 * \return
 *     E_OK on success
 *     E_TIMEOUT if the communication with the firmware times out
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:CreateJobNodeOnController
 * \see \fullref{fct:CreateAllowed
 * \see \fullref{fct:DeleteNodeOnControllerByName
 * \see \fullref{fct:GetNodeType
 * \see \fullref{fct:CreateNodeOnController
 * \see \fullref{fct:NodeCreatedCallbackFunction
 * \see \fullref{fct:NodeCreatedCallbackFunctionExt
 */
EXPORTDLL int CreateNodeOnControllerAsync(HController HC, const char *nodename, VAR_TYPE vartype, int index);

/**
 * Creates a job node on the controller.
 * When this function returns the node on the controller is already created and \texttt{retname} is 
 * filled with the name that the controller returned.
 * \tip{It may not be allowed to create a node on most of the subtrees.
 * Normally it is only advisable to do this in \texttt{usr.var}.}
 * \texttt{E_NOSPACE} is returned.}
 *
 * \param HC is the handle to the controller
 * \param nodename is the full name of the variable, e.g.~textttusr.job.Job.Shape
 * \param jobnodetype e.g.~textttSHAPE
 * \param index is the index in the tree. Only in special cases,,like job trees,,this is other than~texttt0
 * \param retname is the place where the ControllerLib can store the name the firmware returned
 * \param retnamelen Length of the buffer for textttretname
 *
 * \return
 *     E_OK on success
 *     E_NOSPACE if the buffer for retname is too small
 *     E_TIMEOUT if the communication with the firmware times out
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:CreateNodeOnController
 * \see \fullref{fct:CreateAllowed
 * \see \fullref{fct:DeleteNodeOnControllerByName
 * \see \fullref{fct:GetNodeType
 */
EXPORTDLL int CreateJobNodeOnControllerExt(HController HC, const char *nodename, const char *jobnodetype, int index, char *retname, int retnamelen);

/**
 * Creates a job node on the controller.
 * \tip{It may not be allowed to create a node on most of the subtrees.
 * Normally it is only advisable to do this in \texttt{usr.var}.}
 *
 * \param HC is the handle to the controller
 * \param nodename is the full name of a variable, e.g.~textttusr.job.Job.Shape
 * \param jobnodetype is the job node type, e.g.~textttSHAPE
 * \param index is the index in the tree. Only in special cases,,like job trees,,this is other than~texttt0
 *
 * \return
 *     E_OK on success
 *     E_TIMEOUT if the communication with the firmware times out
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:CreateNodeOnController
 * \see \fullref{fct:CreateAllowed
 * \see \fullref{fct:DeleteNodeOnControllerByName
 * \see \fullref{fct:GetNodeType
 */
EXPORTDLL int CreateJobNodeOnController(HController HC, const char *nodename, const char *jobnodetype, int index);

/**
 * Creates a job node on the controller. This call returns immediately and does
 * not wait for notification from the controller. When the node on the
 * controller is actually created this can be catched in a
 * \nameref{fct:NodeCreatedCallbackFunction} or in a 
 * \nameref{fct:NodeCreatedCallbackFunctionExt}. If a lot of nodes have to be
 * created then this function should be used because it is faster than
 * \nameref{fct:CreateNodeOnController}.
 * \tip{It may not be allowed to create a node on most of the subtrees.
 * Normally it is only advisable to do this in \texttt{usr.var}.}
 *
 * \param HC is the handle to the controller
 * \param nodename is the full name of the variable, e.g.~textttusr.job.Job.Shape
 * \param jobnodetype is the job node type, e.g.~textttSHAPE
 * \param index is the index in the tree. Only in special cases,,like job trees,,this is other than~texttt0
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:CreateNodeOnController
 * \see \fullref{fct:CreateAllowed
 * \see \fullref{fct:DeleteNodeOnControllerByName
 * \see \fullref{fct:GetNodeType
 * \see \fullref{fct:NodeCreatedCallbackFunction
 * \see \fullref{fct:NodeCreatedCallbackFunctionExt
 */
EXPORTDLL int CreateJobNodeOnControllerAsync(HController HC, const char *nodename, const char *jobnodetype, int index);

/**
 * Gets the index of NodeObject. This index shows how to order the NodeObjects
 * on a given level. The sorting is ascending. The order of nodes only matters 
 * with the execution of job nodes.
 * \tip{\begin{itemize}\item When the index changes this can be caught with a
 * \nameref{fct:NodeMovedCallbackFunction}.
 * \item With \nameref{fct:GetNodeInfo} it is possible to get all information
 * about a node with a single call.\end{itemize}}
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param index holds the index after returning with textttE_OK
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetNodeInfo
 * \see \fullref{fct:RegisterOnNodeMoved
 * \see \fullref{fct:NodeMovedCallbackFunction
 */
EXPORTDLL int GetNodeIndex(HController HC, HNodeObject HNO, int *index);

/**
 * Gets the type of the \texttt{HNO}. 
 * \tip{\begin{itemize}\item You have to call ReadNode in the first place to get a valid type
 * information.
 * \item The type of a variable can not change.
 * \item This call works only with variables but not with job nodes. Use
 * \nameref{fct:GetNodeTypeString} to get the type for these job nodes.
 * \item With \nameref{fct:GetNodeInfo} it is possible to get all information
 * about a node with a single call.\end{itemize}}
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 *
 * \return
 *     VT_INV unknown type
 *     VT_STR string
 *     VT_TEXT also string
 *     VT_SEL selection
 *     VT_SET set (can have children nodes)
 *     VT_BOOLEAN boolean
 *     VT_BIN binary
 *     VT_I32 32-bit-integer
 *     VT_I64 64-bit-integer
 *     VT_R32 32-bit-real (float)
 *     VT_R64 64-bit-real (double)
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetNodeInfo
 * \see \fullref{fct:GetNodeTypeString
 */
EXPORTDLL int GetNodeType(HController HC, HNodeObject HNO);

/**
 * Gets the length for the type string without the trailing 0-byte.
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param length holds the strlen of the node type after returning with textttE_OK
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetNodeTypeString
 * \see \fullref{fct:GetNodeType
 * \see \fullref{fct:GetNodeInfo
 */
EXPORTDLL int GetNodeTypeStringLen(HController HC, HNodeObject HNO, int *length);

/**
 * Gets the type of the \texttt{HNO} as string. This works on all available nodes.
 * \tip{\begin{itemize}\item You have to call ReadNode in the first place to get a valid type
 * information.
 * \item The type of a variable can not change.
 * \item With \nameref{fct:GetNodeInfo} it is possible to get all information
 * about a node with a single call.\end{itemize}}
 * \begin{lstlisting}[caption={GetNodeTypeString example}]
 * int buflen;
 * if ( GetNodeTypeStringLen(HC, HNO, &buflen) == E_OK ) {
 *   char *buffer = (char*)malloc(++buflen);
 *   if (GetNodeTypeString(HC, HNO, buffer, buflen) == E_OK) {
 *     printf("NodeType: %s\n", buffer);
 *   }
 *   free(buffer);
 * }
 * \end{lstlisting}
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param buffer buffer for the NodeType-string
 * \param len is the length of the buffer (in bytes)
 *
 * \return
 *     E_OK on success
 *     E_NOSPACE if the buffer is too small
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetNodeTypeStringLen
 * \see \fullref{fct:GetNodeType
 * \see \fullref{fct:GetNodeInfo
 */
EXPORTDLL int GetNodeTypeString(HController HC, HNodeObject HNO, char *buffer, int len);

/**
 * Gets the 64-bit-ID of a \texttt{HNO} on the controller. Normally it is
 * not necessary to know this ID in a client application. A variable
 * has this ID over its complete lifetime.
 * \tip{With \nameref{fct:GetNodeInfo} it is possible to get all information
 * about a node with a single call.}
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 *
 * \return
 *     ID of the node
 *     -1 if the ID is unknown
 *
 * \see \fullref{fct:GetNodeInfo
 */
EXPORTDLL int64 GetNodeID(HController HC, HNodeObject HNO);

/**
 * Gets the length of the full node name of a NodeObject.
 * \tip{The length is returned without the trailing 0-byte.}
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 *
 * \return
 *     the strlen of the name
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetNodeName
 * \see \fullref{fct:GetNodeInfo
 */
EXPORTDLL int GetNodeNameLen(HController HC, HNodeObject HNO);

/**
 * Copies the full node name to the buffer, e.g.~\texttt{stat.time.TimeStr}. How to determine the size 
 * of the buffer is explained at \nameref{fct:GetNodeNameLen}.
 * \begin{lstlisting}[caption={GetNodeName example}]
 * int buflen = GetNodeNameLen(HC, HNO);
 * if (buflen > 0) {
 *   char *buffer = (char*)malloc(++buflen);
 *   if (GetNodeName(HC, HNO, buffer, buflen) == E_OK) {
 *     printf("NodeName: %s\\n", buffer);
 *   }
 *   free(buffer);
 * }
 * \end{lstlisting}
 * \tip{With \nameref{fct:GetNodeInfo} it is possible to get all information
 * about a node with a single call.}
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param buffer is the buffer for the name of the NodeObject
 * \param len it the length of the buffer in bytes
 *
 * \return
 *     E_OK on success
 *     E_NOSPACE if the buffer is too small
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetNodeNameLen
 * \see \fullref{fct:GetNodeLastName
 * \see \fullref{fct:GetNodeInfo
 */
EXPORTDLL int GetNodeName(HController HC, HNodeObject HNO, char *buffer, int len);

/**
 * Gets the length of the last node name of a NodeObject.
 * \tip{The length is returned without the trailing 0-byte.}
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 *
 * \return
 *     the strlen of the name
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetNodeLastName
 * \see \fullref{fct:GetNodeName
 * \see \fullref{fct:GetNodeInfo
 */
EXPORTDLL int GetNodeLastNameLen(HController HC, HNodeObject HNO);

/**
 * Copies the last node name to the buffer; e.g.~\texttt{TimeStr}. How to determine the size 
 * of the buffer is explained at \nameref{fct:GetNodeLastNameLen}.
 * \begin{lstlisting}[caption={GetNodeLastName example}]
 * int buflen = GetNodeLastNameLen(HC, HNO);
 * if (buflen > 0) {
 *   char *buffer = (char*)malloc(++buflen);
 *   if (GetNodeLastName(HC, HNO, buffer, buflen) == E_OK) {
 *     printf("Last-NodeName: %s\\n", buffer);
 *   }
 *   free(buffer);
 * }
 * \end{lstlisting}
 * \tip{With \nameref{fct:GetNodeInfo} it is possible to get all information
 * about a node with a single call.}
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param buffer is the buffer for the name of the NodeObject
 * \param len is the length of the buffer in bytes
 *
 * \return
 *     E_OK on success
 *     E_NOSPACE if the buffer is too small
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetNodeNameLen
 * \see \fullref{fct:GetNodeLastName
 * \see \fullref{fct:GetNodeInfo
 */
EXPORTDLL int GetNodeLastName(HController HC, HNodeObject HNO, char *buffer, int len);

/**
 * Checks, whether it is allowed to modify the value of the NodeObject. This function should be called
 * before any modification to the value of the NodeObject.
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param allowed holds texttt1
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 */
EXPORTDLL int ModifyAllowed(HController HC, HNodeObject HNO, int *allowed);

/**
 * Checks, whether it is allowed to rename the NodeObject. 
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param allowed holds texttt1
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 */
EXPORTDLL int RenameAllowed(HController HC, HNodeObject HNO, int *allowed);

/**
 * Checks, whether it is allowed to delete the NodeObject. 
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param allowed holds texttt1
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:DeleteNodeOnControllerByName
 */
EXPORTDLL int DeleteAllowed(HController HC, HNodeObject HNO, int *allowed);

/**
 * Checks, whether it is allowed to create subnodes in the NodeObject. 
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param allowed holds texttt1
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:CreateNodeOnController
 */
EXPORTDLL int CreateAllowed(HController HC, HNodeObject HNO, int *allowed);

/**
 * Checks, whether it is allowed to modify the flags of the NodeObject. 
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param allowed holds texttt1
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 */
EXPORTDLL int FlagsModifyAllowed(HController HC, HNodeObject HNO, int *allowed);

/**
 * Checks, whether it is allowed to extend \texttt{VAR:SELECT}-variables by an user entry.
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param allowed holds texttt1
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 */
EXPORTDLL int ExtendRangeAllowed(HController HC, HNodeObject HNO, int *allowed);

/**
 * Checks, whether it is allowed to
 * modify the minimum value and the maximum value of the NodeObject. 
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param allowed holds texttt1
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 */
EXPORTDLL int ModifyMinMaxAllowed(HController HC, HNodeObject HNO, int *allowed);

/**
 * Checks, whether it is allowed to modify the unit of the NodeObject. 
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param allowed holds texttt1
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 */
EXPORTDLL int ModifyUnitAllowed(HController HC, HNodeObject HNO, int *allowed);

/**
 * Checks, whether the NodeObject is consumable.
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param consumable holds texttt1
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:SetConsumable
 * \see \fullref{fct:IsUserdefined
 */
EXPORTDLL int IsConsumable(HController HC, HNodeObject HNO, int *consumable);

/**
 * Sets the NodeObject as \texttt{consumable}.
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param consumable set this to texttt1
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:IsConsumable
 * \see \fullref{fct:FlagsModifyAllowed
 */
EXPORTDLL int SetConsumable(HController HC, HNodeObject HNO, int consumable);

/**
 * Checks, whether the consumable NodeObject can be used in a pen.
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param penable holds texttt1
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:IsForcePen
 */
EXPORTDLL int IsPenable(HController HC, HNodeObject HNO, int *penable);

/**
 * Checks, whether the NodeObject is controlled by a device.
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param controlled holds texttt1
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:SetControlledByDevice
 */
EXPORTDLL int IsControlledByDevice(HController HC, HNodeObject HNO, int *controlled);

/**
 * Sets the NodeObject as controlled by a device.
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param controlled set this to texttt1
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:IsControlledByDevice
 * \see \fullref{fct:FlagsModifyAllowed
 */
EXPORTDLL int SetControlledByDevice(HController HC, HNodeObject HNO, int controlled);

/**
 * Checks, whether the NodeObject is owned by a device.
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param owned holds texttt1
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:IsControlledByDevice
 */
EXPORTDLL int IsOwnedByDevice(HController HC, HNodeObject HNO, int *owned);

/**
 * Checks, whether the NodeObject is
 * protected. This means that the controller does not allow
 * the variable to be changed by the user.
 * \tip{Please use \nameref{fct:ModifyAllowed} to find out whether a variable can
 * be modified or not, because it is not safe to rely on this flag only.}
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param prot holds texttt1
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:ModifyAllowed
 * \see \fullref{fct:IsWriteable
 */
EXPORTDLL int IsProtected(HController HC, HNodeObject HNO, int *prot);

/**
 * Checks, whether the NodeObject has been modified. If the NodeObject has the modified-flag then its
 *  value differs from the value in the NVRAM.
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param modified holds texttt1
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:WriteToNVRAM
 * \see \fullref{fct:IsMirrored
 */
EXPORTDLL int IsModified(HController HC, HNodeObject HNO, int *modified);

/**
 * Checks, whether the NodeObject is mirrored in the NVRAM. 
 * This means that the value can be saved and is
 * automatically restored on controller startup.
 * \tip{To actually save mirrored NodeObjects to the NVRAM use
 * \nameref{fct:WriteToNVRAM}. If NodeObjects shall always write their
 * current value then see \nameref{fct:SetQuicksaveable}.}
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param mirrored holds texttt1
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:SetMirrored
 * \see \fullref{fct:IsQuicksaveable
 * \see \fullref{fct:WriteToNVRAM
 */
EXPORTDLL int IsMirrored(HController HC, HNodeObject HNO, int *mirrored);

/**
 * Sets the NodeObject to be mirrored in the NVRAM. If a 
 * NodeObject is mirrored in the NVRAM then the current value can be saved with a call
 * to \nameref{fct:WriteToNVRAM} and the value will be automatically restored on
 * controller startup.
 * \tip{To actually save mirrored NodeObjects to the NVRAM use
 * \nameref{fct:WriteToNVRAM}. If NodeObjects shall always write their
 * current value without the need of \nameref{fct:WriteToNVRAM} then 
 * see \nameref{fct:SetQuicksaveable}.}
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param mirrored set this totexttt1
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:IsMirrored
 * \see \fullref{fct:IsQuicksaveable
 * \see \fullref{fct:WriteToNVRAM
 * \see \fullref{fct:FlagsModifyAllowed
 */
EXPORTDLL int SetMirrored(HController HC, HNodeObject HNO, int mirrored);

/**
 * Checks, whether the NodeObject is quicksaveable. 
 * This means, when the value of the NodeObject changes then the new 
 * value will automatically be written to the NVRAM without the need of calling
 * \nameref{fct:WriteToNVRAM}.
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param quicksaveable holds texttt1
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:SetQuicksaveable
 * \see \fullref{fct:IsMirrored
 * \see \fullref{fct:WriteToNVRAM
 */
EXPORTDLL int IsQuicksaveable(HController HC, HNodeObject HNO, int *quicksaveable);

/**
 * Sets the NodeObject as quicksaveable. 
 * This means, when the value of the NodeObject changes then the new 
 * value will automatically be written to the NVRAM without the need of calling
 * \nameref{fct:WriteToNVRAM}.
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param quicksaveable set this to texttt1
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:IsQuicksaveable
 * \see \fullref{fct:SetMirrored
 * \see \fullref{fct:WriteToNVRAM
 * \see \fullref{fct:FlagsModifyAllowed
 */
EXPORTDLL int SetQuicksaveable(HController HC, HNodeObject HNO, int quicksaveable);

/**
 * Checks,\,whether a NodeObject in a pen forces the pen-mechanism to change immediately when the pen is
 * activated. Otherwise the change would not apply until the value is needed for
 * the first time during job execution.
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param force holds texttt1
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:SetForcePen
 */
EXPORTDLL int IsForcePen(HController HC, HNodeObject HNO, int *force);

/**
 * Sets the NodeObject in a pen to force the pen-mechanism to change immediately once the pen is
 * activated. Otherwise the change would not apply until the value is needed for
 * the first time during job execution.
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param force set this to texttt1
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:IsForcePen
 * \see \fullref{fct:FlagsModifyAllowed
 */
EXPORTDLL int SetForcePen(HController HC, HNodeObject HNO, int force);

/**
 * Checks, whether the NodeObject was created by the user.
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param userdefined holds texttt1
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 */
EXPORTDLL int IsUserdefined(HController HC, HNodeObject HNO, int *userdefined);

/**
 * Checks, whether the NodeObject can be written by the user. 
 * In difference to \nameref{fct:IsProtected} this flag can be set by the user.
 * \tip{Please use \nameref{fct:ModifyAllowed} to evaluate whether a variable can
 * be modified or not, because it is not safe to rely on this flag only.}
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param writeable holds texttt1
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:ModifyAllowed
 */
EXPORTDLL int IsWriteable(HController HC, HNodeObject HNO, int *writeable);

/**
 * Checks, whether the NodeObject is a job-node which can be created by the user, like \texttt{JOB:POLYGON}.
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param allowed holds texttt1
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 */
EXPORTDLL int UserCreatable(HController HC, HNodeObject HNO, int *allowed);

/**
 * Checks, whether the NodeObject is a job-node which outputs lines, like \newline \texttt{JOB:POLYGON}.
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param allowed holds texttt1
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 */
EXPORTDLL int CanMakeLines(HController HC, HNodeObject HNO, int *allowed);

/**
 * Checks, whether the NodeObject is a job-node which can pass lines, like \newline \texttt{JOB:REPEAT}.
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param allowed holds texttt1
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 */
EXPORTDLL int CanPassLines(HController HC, HNodeObject HNO, int *allowed);

/**
 * Checks, whether the NodeObject is a job-node which accepts subnodes, like \newline \texttt{JOB:REPEAT}
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param allowed holds texttt1
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 */
EXPORTDLL int AcceptsSubnodes(HController HC, HNodeObject HNO, int *allowed);

/**
 * If the NodeObject is of the type \texttt{VT_SEL} (\texttt{VAR:SELECT}) then
 * this function gets the number of its entries. Use \nameref{fct:GetSelectEntry} to
 * retrieve an entry at index \texttt{n}.
 * \tip{To get the currently selected entry call \nameref{fct:GetNodeValueString}.}
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param count holds the number of entries after returning with textttE_OK
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetSelectEntryLength
 * \see \fullref{fct:GetSelectEntry
 * \see \fullref{fct:GetNodeInfo
 * \see \fullref{fct:GetNodeValueString
 */
EXPORTDLL int GetSelectEntriesCount(HController HC, HNodeObject HNO, int *count);

/**
 * If the NodeObject is of type \texttt{VT_SEL} (\texttt{VAR:SELECT}) then this
 * function gets the string length of the entry at \texttt{index}. 
 * \tip{To get the currently selected entry call \nameref{fct:GetNodeValueString}.}
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param index is the entry number
 * \param length holds the textttstrlen
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetNodeValueString
 * \see \fullref{fct:GetSelectEntry
 * \see \fullref{fct:GetSelectEntriesCount
 * \see \fullref{fct:GetNodeInfo
 */
EXPORTDLL int GetSelectEntryLength(HController HC, HNodeObject HNO, int index, int *length);

/**
 * If the NodeObject is of type \texttt{VT_SEL} (\texttt{VAR:SELECT}) then this
 * function gets the string of the entry at \texttt{index}. 
 * \tip{To get the currently selected entry use \nameref{fct:GetNodeValueString}.}
 * \begin{lstlisting}[caption={GetSelectEntry example}]
 * int cnt;
 * if ( GetSelectEntriesCount(HC, HNO, &cnt) == E_OK ) {
 *   for (int i=0; i<cnt; ++i) {
 *     int buflen;
 *     if ( GetSelectEntryLength(HC, HNO, i, &buflen) == E_OK ) {
 *       char *buffer = (char*)malloc(++buflen);
 *       if (GetSelectEntry(HC, HNO, i, buffer, buflen) == E_OK) {
 *         printf("Entry %i: %s\n", i, buffer);
 *       }
 *       free(buffer);
 *     }
 *   }
 * }
 * \end{lstlisting}
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param index is the entry number
 * \param buffer is the buffer for the entry
 * \param bufferlen is the length of the buffer
 *
 * \return
 *     E_OK on success
 *     E_NOSPACE if the buffer is too small
 *     E_INVALID if the variable is not of type textttVT_SEL
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetNodeValueString
 * \see \fullref{fct:GetSelectEntryLength
 * \see \fullref{fct:GetSelectEntriesCount
 * \see \fullref{fct:GetNodeInfo
 */
EXPORTDLL int GetSelectEntry(HController HC, HNodeObject HNO, int index, char *buffer, int bufferlen);

/**
 * If the NodeObject is of type \texttt{VT_BIN} (\texttt{VAR:BIN}) then this
 * function sets a new value.
 * \tip{The buffer is copied so it can be freed immediately.}
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param buffer is the pointer to the data
 * \param leninbytes is the length of the data in bytes
 *
 * \return
 *     E_OK on success
 *     E_NALLOWED if the NodeObject is not modifyable
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetNodeValueBinCopy
 * \see \fullref{fct:GetNodeValueBinCopyLen
 * \see \fullref{fct:ModifyAllowed
 */
EXPORTDLL int SetNodeValueBin(HController HC, HNodeObject HNO,const unsigned char *buffer, int leninbytes);

/**
 * If the NodeObject is of type \texttt{VT_BIN} (\texttt{VAR:BIN}) then this
 * function gets the size in bytes. 
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 *
 * \return
 *     the size of the variable in bytes
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetNodeValueBinCopy
 * \see \fullref{fct:SetNodeValueBin
 */
EXPORTDLL int GetNodeValueBinCopyLen(HController HC, HNodeObject HNO);

/**
 * If the NodeObject is of type \texttt{VT_BIN} (\texttt{VAR:BIN}) then this
 * function gets a copy of the data. The buffer has to have the size of \texttt{len} bytes. 
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param buffer holds the buffer where the node value gets copied in after returning with textttE_OK
 * \param len is the size of the buffer in bytes
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetNodeValueBinCopyLen
 * \see \fullref{fct:SetNodeValueBin
 */
EXPORTDLL int GetNodeValueBinCopy(HController HC, HNodeObject HNO, unsigned char *buffer, int len);

/**
 * If the NodeObject is of type \texttt{VT_BOOLEAN} (\texttt{VAR:BOOLEAN}) then this
 * function sets its new value.
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param flag is the new value
 *
 * \return
 *     E_OK on success
 *     E_NALLOWED if the NodeObject is not modifyable
 *     E_INVALID if the node is not of type textttVT_BOOLEAN
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetNodeValueBool
 * \see \fullref{fct:ModifyAllowed
 */
EXPORTDLL int SetNodeValueBool(HController HC, HNodeObject HNO, int flag);

/**
 * If the NodeObject is of type \texttt{VT_BOOLEAN} (\texttt{VAR:BOOLEAN}) then this
 * function gets its value.
 * \tip{With \nameref{fct:GetNodeInfo} it is possible to get all information
 * about a node with a single call.}
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param flag is the new value
 *
 * \return
 *     E_OK on success
 *     E_INVALID if the node is not of type textttVT_BOOLEAN
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:SetNodeValueBool
 * \see \fullref{fct:GetNodeInfo
 */
EXPORTDLL int GetNodeValueBool(HController HC, HNodeObject HNO, int *flag);

/**
 * Returns the \texttt{strlen} of the value.
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 *
 * \return
 *     the textttstrlen
 *     E_INVALID if the node is not representable as a string
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetNodeValueString
 * \see \fullref{fct:GetNodeInfo
 */
EXPORTDLL int GetNodeValueStringLen(HController HC, HNodeObject HNO);

/**
 * Gets the string value of a NodeObject. This function can
 * be called for NodeObjects of the following types:
 * \begin{itemize}
 *   \item VT_STR (VAR:STRING)
 *   \item VT_TEXT (VAR:TEXT)
 *   \item VT_I32 (VAR:INT32)
 *   \item VT_I64 (VAR:INT64)
 *   \item VT_R32 (VAR:REAL32)
 *   \item VT_R64 (VAR:REAL64)
 *   \item VT_SEL (VAR:SELECT)
 *   \item VT_BOOLEAN (VAR:BOOLEAN)
 * \end{itemize}
 * 
 * If the function returns \texttt{E_OK} then the string value can be read from
 * \texttt{value}. Call \nameref{fct:GetNodeValueStringLen} to find out how big the
 * buffer has to be.
 * \begin{lstlisting}[caption={GetNodeValueString example}]
 * int buflen = GetNodeValueStringLen(HC, HNO);
 * if (buflen > 0) {
 *   char *buffer = (char*)malloc(++buflen);
 *   if (GetNodeValueString(HC, HNO, buffer, buflen) == E_OK) {
 *     printf("Nodevalue: \%s\n", buffer);
 *   }
 *   free(buffer);
 * }
 * \end{lstlisting}
 * \tip{\begin{itemize}\item The decimal separator is a dot by default. To change this to your current locale call 
 * \nameref{fct:DisableFloatingPointToStringDot}.
 * \item With \nameref{fct:GetNodeInfo} it is possible to get all information
 * about a node with a single call.\end{itemize}}
 * \todo{holds ... ?}
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param value pointer to a buffer
 * \param maxlen size of the buffer
 *
 * \return
 *     E_OK on success
 *     E_NOSPACE if the buffer is too small
 *     E_INVALID if the node is not representable as a string
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetNodeValueStringLen
 * \see \fullref{fct:SetNodeValueString
 * \see \fullref{fct:GetNodeInfo
 */
EXPORTDLL int GetNodeValueString(HController HC, HNodeObject HNO, char *value, int maxlen);

/**
 * Gets the int32-value of the NodeObject. Only valid if \nameref{fct:GetNodeType}
 * returned \texttt{VT_I32}. If the function returns \texttt{E_OK} then
 * \texttt{value} has the current value.
 * \tip{With \nameref{fct:GetNodeInfo} it is possible to get all information
 * about a node with a single call.}
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param value holds the value after the call returns with textttE_OK
 *
 * \return
 *     E_OK on success
 *     E_INVALID if the node is not a int32-node
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:SetNodeValueInt32
 * \see \fullref{fct:GetNodeType
 * \see \fullref{fct:GetNodeInfo
 */
EXPORTDLL int GetNodeValueInt32(HController HC, HNodeObject HNO, int *value);

/**
 * Gets the int64-value of a NodeObject. Only valid if \nameref{fct:GetNodeType}
 * returned \texttt{VT_I64}. If the function returns \texttt{E_OK} then
 * \texttt{value} has the current value.
 * \tip{With \nameref{fct:GetNodeInfo} it is possible to get all information
 * about a node with a single call.}
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param value holds the value after the call returns with textttE_OK
 *
 * \return
 *     E_OK on success
 *     E_INVALID if the node is not a int64-node
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:SetNodeValueInt64
 * \see \fullref{fct:GetNodeType
 * \see \fullref{fct:GetNodeInfo
 */
EXPORTDLL int GetNodeValueInt64(HController HC, HNodeObject HNO, int64 *value);

/**
 * Gets the real32-value of a NodeObject. Only valid if
 * \nameref{fct:GetNodeType} returned \texttt{VT_R32}. If the function returns
 * \texttt{E_OK} then \texttt{value} has the current value.
 * \tip{With \nameref{fct:GetNodeInfo} it is possible to get all information
 * about a node with a single call.}
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param value holds the value after the call returns with textttE_OK
 *
 * \return
 *     E_OK on success
 *     E_INVALID if the node is not of type real32
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:SetNodeValueReal32
 * \see \fullref{fct:GetNodeType
 * \see \fullref{fct:GetNodeInfo
 */
EXPORTDLL int GetNodeValueReal32(HController HC, HNodeObject HNO, float *value);

/**
 * Gets the real64-value of a NodeObject. Only valid if
 * \nameref{fct:GetNodeType} returned \texttt{VT_R64}. If the function returns
 * \texttt{E_OK} then \texttt{value} has the current value.
 * \tip{With \nameref{fct:GetNodeInfo} it is possible to get all information
 * about a node with a single call.}
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param value holds the value after the call returns with textttE_OK
 *
 * \return
 *     E_OK on success
 *     E_INVALID if the node is not of type real64
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:SetNodeValueReal32
 * \see \fullref{fct:GetNodeType
 * \see \fullref{fct:GetNodeInfo
 */
EXPORTDLL int GetNodeValueReal64(HController HC, HNodeObject HNO, double *value);

/**
 * Sets the string-value of the NodeObject if the NodeObject is of type \newline \texttt{VT_STR}, 
 * \texttt{VT_TEXT}, \texttt{VT_I32}, \texttt{VT_I64},
 * \texttt{VT_R32}, \texttt{VT_R64}, \texttt{VT_SEL} or \texttt{VT_BOOLEAN}.
 * \tip{\begin{itemize}\item The decimal separator is a dot by default. To change this to your current locale call 
 * \nameref{fct:DisableFloatingPointToStringDot}.
 * \item To apply the changes to the controller you have to call \nameref{fct:WriteNode}.\end{itemize}}
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param value is the new string-value of the NodeObject
 *
 * \return
 *     E_OK on success
 *     E_NALLOWED if it is not allowed to modify the NodeObject
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:DisableFloatingPointToStringDot
 * \see \fullref{fct:GetNodeValueString
 * \see \fullref{fct:WriteNode
 * \see \fullref{fct:ModifyAllowed
 */
EXPORTDLL int SetNodeValueString(HController HC, HNodeObject HNO, const char *value);

/**
 * Sets the int32-value of the NodeObject.
 * \tip{To apply the changes to the controller you have to call \nameref{fct:WriteNode}.}
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param value is the new int32-value of the NodeObject
 *
 * \return
 *     E_OK on success
 *     E_NALLOWED if it is not allowed to modify the NodeObject
 *     E_RANGE if the value is less than the minimum value or greater than the maximum value
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetNodeValueInt32
 * \see \fullref{fct:WriteNode
 * \see \fullref{fct:ModifyAllowed
 * \see \fullref{fct:GetMin
 * \see \fullref{fct:GetMax
 */
EXPORTDLL int SetNodeValueInt32(HController HC, HNodeObject HNO, int value);

/**
 * Sets the int64-value of a NodeObject.
 * \tip{To apply the changes to the controller you have to call \nameref{fct:WriteNode}.}
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param value is the new int64-value of the NodeObject
 *
 * \return
 *     E_OK on success
 *     E_NALLOWED if it is not allowed to modify the NodeObject
 *     E_RANGE if the value is less than the minimum value or greater than the maximum value
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetNodeValueInt64
 * \see \fullref{fct:WriteNode
 * \see \fullref{fct:ModifyAllowed
 * \see \fullref{fct:GetMin
 * \see \fullref{fct:GetMax
 */
EXPORTDLL int SetNodeValueInt64(HController HC, HNodeObject HNO, int64 value);

/**
 * Sets the real32-value of the NodeObject.
 * \tip{To apply the changes to the controller you have to call \emph{WriteNode(HC, HNO)}.}
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param value is the new real32-value of the NodeObject
 *
 * \return
 *     E_OK on success
 *     E_NALLOWED if it is not allowed to modify the NodeObject
 *     E_RANGE if the value is less than the minimum value or greater than the maximum value
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetNodeValueReal32
 * \see \fullref{fct:WriteNode
 * \see \fullref{fct:ModifyAllowed
 * \see \fullref{fct:GetMin
 * \see \fullref{fct:GetMax
 */
EXPORTDLL int SetNodeValueReal32(HController HC, HNodeObject HNO, float value);

/**
 * Sets the real64-value of the NodeObject.
 * \tip{To apply the changes to the controller you have to call \nameref{fct:WriteNode}.}
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param value is the new real64-value of the NodeObject
 *
 * \return
 *     E_OK on success
 *     E_NALLOWED if it is not allowed to modify the NodeObject
 *     E_RANGE if the value is less than the minimum value or greater than the maximum value
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetNodeValueReal64
 * \see \fullref{fct:WriteNode
 * \see \fullref{fct:ModifyAllowed
 * \see \fullref{fct:GetMin
 * \see \fullref{fct:GetMax
 */
EXPORTDLL int SetNodeValueReal64(HController HC, HNodeObject HNO, double value);

/**
 * Gets the minimum value of the NodeObject. This value is set by the controller
 * and cannot be changed by the user. If the minimum value is greater
 * than the maximum value then the value itself is unlimited.
 * \tip{With \nameref{fct:GetNodeInfo} it is possible to get all information
 * about a node with a single call.}
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param min holds the minimum value of the NodeObject after returning with textttE_OK
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetMax
 * \see \fullref{fct:GetNodeInfo
 */
EXPORTDLL int GetMin(HController HC, HNodeObject HNO, float *min);

/**
 * Gets the maximum value of the NodeObject. This value is set by the controller
 * and cannot be changed by the user. If the minimum value is greater
 * than the maximum value then the value itself is unlimited.
 * \tip{With \nameref{fct:GetNodeInfo} it is possible to get all information
 * about a node with a single call.}
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param max holds the maximum value of the NodeObject after returning with textttE_OK
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetMin
 * \see \fullref{fct:GetNodeInfo
 */
EXPORTDLL int GetMax(HController HC, HNodeObject HNO, float *max);

/**
 * Gets the needed buffersize for the unit-string. Note, that the
 * needed buffersize is without the trailing 0-byte.
 * \tip{With \nameref{fct:GetNodeInfo} it is possible to get all information
 * about a node with a single call.}
 * needed buffersize is without the trailing 0-byte}
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param len holds the needed buffersize after returning with textttE_OK
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetUnit
 * \see \fullref{fct:GetNodeInfo
 */
EXPORTDLL int GetUnitLen(HController HC, HNodeObject HNO, int *len);

/**
 * Gets the unit for the given NodeObject as string.
 * \begin{lstlisting}[caption={GetUnit example}]
 * int len;
 * if ( GetUnitLen(HC, HNO, &len) == E_OK ) {
 *   if ( len > 0 ) {
 *     char *buf = (char*)malloc(sizeof(char)*(++len));
 *     if ( GetUnit(HC, HNO, buf, len) == E_OK ) {
 *       printf("Unit: %s\n", buf);
 *     }
 *     free(buf);
 *   }
 * }
 * \end{lstlisting}
 * 
 * \tip{With \nameref{fct:GetNodeInfo} it is possible to get all information
 * about a node with a single call.}
 * \todo{Ergänzen? holds the unit as string after returning with \texttt{E_OK}}
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param unit is the pointer to a buffer big enough for the unit
 * \param len is the buffersize
 *
 * \return
 *     E_OK on success
 *     E_NOMEM buffer too small
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetUnitLen
 * \see \fullref{fct:GetNodeInfo
 * \see \fullref{fct:GetNodeInfoExt
 */
EXPORTDLL int GetUnit(HController HC, HNodeObject HNO, char *unit, int len);

/**
 * Gets the strlen of all subnodes of the given node. To get the subnodes
 * call \nameref{fct:GetSubnodesNames}.
 *
 * \param HC is the handle to the controller
 * \param nodename is the node from which the subnodes are wanted
 * \param len holds the strlen of the job names after returning with textttE_OK
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetSubnodesNames
 */
EXPORTDLL int GetSubnodesNamesLen(HController HC, const char *nodename, int *len);

/**
 * Gets the names of all subnodes of the given node. The names of the subnodes are separated by
 * semicolons (e.g. \texttt{"usr.var.x;usr.var.y0"}).
 * \begin{lstlisting}[caption={GetSubnodesNames example}]
 * int buflen;
 * if ( GetSubnodesNamesLen(HC, HNO, &buflen) == E_OK ) {
 *   char *buffer = (char*)malloc(++buflen);
 *   if (GetSubnodesNames(HC, HNO, buffer, buflen) == E_OK) {
 *     printf("NodeName: %s\n", buffer);
 *   }
 *   free(buffer);
 * }
 * \end{lstlisting}
 *
 * \param HC is the handle to the controller
 * \param nodename is the node from which the subnodes are wanted
 * \param buffer is the pointer to the buffer for the subnode names
 * \param len is the size of the buffer
 *
 * \return
 *     E_OK on success
 *     E_NOSPACE if the buffer is too small
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetSubnodesNamesLen
 */
EXPORTDLL int GetSubnodesNames(HController HC, const char *nodename, char *buffer, int len);

/**
 * Returns all subnodes as HNodeObjects in a single call.
 * \begin{lstlisting}[caption={GetSubnodesHNOArray example}]
 * int subnodecount = GetSubnodesCount(HC, HNO);
 * HNodeObject *subnodes = (HNodeObject*)malloc(sizeof(HNodeObject)*subnodecount);
 * int ret = GetSubnodesHNOArray(HC, HNO, subnodes, &subnodecount);
 * 
 * if ( ret != E_OK ) {
 *   return -1;
 * }
 * 
 * for (int i=0; i< subnodecount; ++i) {
 *   ARG_NODEINFO *info = GetNodeInfo(HC, subnodes[i]);
 *   if ( info ) {
 *     printf("  %s\n", info->name);
 *     DestroyNodeInfo(info);
 *   }
 * }
 * \end{lstlisting}
 *
 * \param HC is the handle to the controller
 * \param HNOparent is the node of which the subnodes are wanted
 * \param buffer pointer to a buffer for the HNOs
 * \param count size of the buffer. On return it holds the actual number of elements
 *
 * \return
 *     E_OK on success
 *     E_NOSPACE if the buffer is too small
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetSubnodesCount
 */
EXPORTDLL int GetSubnodesHNOArray(HController HC, HNodeObject HNOparent, HNodeObject *buffer, int *count );

/**
 * Tests, whether a node is the child of another node or not.
 * \begin{lstlisting}[caption={IsNodeChildOf example}]
 * parentHandle = GetNode(HC, "usr.job");
 * childHandle = GetNode(HC, "usr.job.Job");
 * if ( IsNodeChildOf(HC, childHandle, parentHandle) == E_OK ) {
 *   // usr.job.Job is a child of usr.job!
 * }
 * \end{lstlisting}
 *
 * \param HC is the handle to the controller
 * \param childHandle is the handle to the assumed child node
 * \param parentHandle is the handle to the assumed parent node
 *
 * \return
 *     E_OK if childHandle is a child of parentHandle
 *     E_INVALID if childHandle is not a child of parentHandle
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:IsNodeDescendantOf
 */
EXPORTDLL int IsNodeChildOf(HController HC, HNodeObject childHandle, HNodeObject parentHandle);

/**
 * Tests, whether a node is the descendant of another node or not.
 * \begin{lstlisting}[caption={IsNodeDescendantOf example}]
 * parentHandle = GetNode(HC, "usr.job");
 * descendantHandle = GetNode(HC, "usr.job.Job.Shape");
 * if ( IsNodeDescendantOf(HC, descendantHandle, parentHandle) == E_OK ) {
 *   // usr.job.Job.Shape is a descendant of usr.job!
 * }
 * \end{lstlisting}
 *
 * \param HC is the handle to the controller
 * \param descendantHandle is the handle to the assumed descendant node
 * \param parentHandle is the handle to the assumed parent node
 *
 * \return
 *     E_OK if descendantHandle is a descendant of parentHandle
 *     E_INVALID if descendantHandle is not a descendant of parentHandle
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:IsNodeChildOf
 */
EXPORTDLL int IsNodeDescendantOf(HController HC, HNodeObject descendantHandle, HNodeObject parentHandle);

/**
 * Gets the editor hint for a variable. This is only useful when \nameref{fct:SetAttributes}
 * was called before.
 *
 * \param HC is the handle to the controller
 * \param HNO handle the NodeObject
 * \param hint holds the editor hint after returning with textttE_OK
 *
 * \return
 *     E_OK if descendantHandle is a descendant of parentHandle
 *     E_UNIMP if the firmware does not support this feature
 *     
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:SetAttributes
 */
EXPORTDLL int GetEditorHint(HController HC, HNodeObject HNO, char hint[128]);

/**
 * Creates a NodeObjectCollection. With NodeObjectCollections writing of more than 1~NodeObject simultaneously is usually faster.
 *
 * \param HC is the handle to the controller
 *
 * \return
 *     handle to the collection
 *     ARG_INVALID_HANDLE_VALUE if not successfull
 *
 * \see \fullref{fct:DestroyNodeObjectCollection
 */
EXPORTDLL HNodeObjectCollection CreateNodeObjectCollection(HController HC);

/**
 * Destroys a NodeObjectCollection. All NodeObjects in the Collection get also destroyed.
 *
 * \param HC is the handle to the controller
 * \param HNOC is the handle to the NodeObjectCollection
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:CreateNodeObjectCollection
 */
EXPORTDLL int DestroyNodeObjectCollection(HController HC, HNodeObjectCollection HNOC);

/**
 * Adds a NodeObject to a collection.
 * \tip{For performance reasons it is recommended to use 
 * \nameref{fct:AddNodeObjectByName} to add NodeObjects to the NodeObjectCollection.}
 *
 * \param HC is the handle to the controller
 * \param HNOC is the handle to the NodeObjectCollection
 * \param HNO is the handle to the NodeObject
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:AddNodeObjectByName
 * \see \fullref{fct:AddNodeObjectSubtreeByName
 * \see \fullref{fct:RemoveNodeObject
 */
EXPORTDLL int AddNodeObject(HController HC, HNodeObjectCollection HNOC, HNodeObject HNO);

/**
 * Adds a NodeObject to a collection.
 *
 * \param HC is the handle to the controller
 * \param HNOC is the handle to the NodeObjectCollection
 * \param varname is the complete path of the node
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:AddNodeObject
 * \see \fullref{fct:AddNodeObjectSubtreeByName
 * \see \fullref{fct:RemoveNodeObject
 */
EXPORTDLL int AddNodeObjectByName(HController HC, HNodeObjectCollection HNOC, const char *varname);

/**
 * Adds the subtree of the given node to NodeObjectCollection. 
 * \texttt{AddNodeObjectSubtreeByName(HC, HNOC, "usr", 9999, callback)} \newline to get the complete \texttt{usr.*} subtree.
 *
 * \param HC is the handle to the controller
 * \param HNOC is the handle to the NodeObjectCollection
 * \param nodename is the name of the subtree
 * \param level defines how deep the tree structure shall be loaded where texttt1
 * \param callback if this is not textttNULL
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:AddNodeObject
 * \see \fullref{fct:AddNodeObjectByName
 * \see \fullref{fct:RemoveNodeObject
 * \see \fullref{fct:ValueChangeCallbackFunction
 */
EXPORTDLL int AddNodeObjectSubtreeByName(HController HC, HNodeObjectCollection HNOC, const char *nodename, int level,ValueChangeCallbackFunction callback);

/**
 * Fills \texttt{HNOC} with the children of \texttt{HNO}. 
 * \tip{\begin{itemize}\item This function only works correct if the variable cache is enabled
 * with \nameref{fct:EnableVariableCache}.
 * \item The NodeObjectCollection should not contain any elements.\end{itemize}}
 * \begin{lstlisting}[caption={GetSubnodes example}]
 * // Print subnodes of usr.job
 * int cnt, len;
 * HNodeObjectCollection HNOC;
 * HNodeObject HNO;
 * char *str;
 * 
 * HNOC = CreateNodeObjectCollection(HC);
 * if ( HNOC == ARG_INVALID_HANDLE_VALUE ) { error(); }
 * HNO = GetNode(HC, "usr.job");
 * if ( HNO == ARG_INVALID_HANDLE_VALUE ) { error(); }
 * if ( GetSubnodes(HC, HNO, HNOC) != E_OK ) { error(); }
 * if ( GetNodeObjectCount(HC, HNOC, &cnt) != E_OK) { error(); }
 * for (int i=0; i<cnt; ++i) {
 *   if ( GetNodeObjectAtIndex(HC, HNOC, i, &HNO) != E_OK ) { error(); }
 *   len = GetNodeNameLen(HC, HNO);
 *   if ( len < 1 ) { error(); }
 *   str = (char*)malloc(++len);
 *   if ( GetNodeName(HC, HNO, str, len) != E_OK ) { error(); }
 *   printf("%s", str);
 *   free(str);
 * }
 * 
 * DestroyNodeObjectCollection(HC, HNOC);
 * \end{lstlisting}
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param HNOC is the handle to the NodeObjectCollection
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:EnableVariableCache
 * \see \fullref{fct:GetParentNode
 * \see \fullref{fct:CreateNodeObjectCollection
 * \see \fullref{fct:GetNode
 * \see \fullref{fct:GetNodeObjectCount
 */
EXPORTDLL int GetSubnodes(HController HC, HNodeObject HNO, HNodeObjectCollection HNOC);

/**
 * Removes a NodeObject from a collection without destroying the NodeObject.
 *
 * \param HC is the handle to the controller
 * \param HNOC is the handle to the NodeObjectCollection
 * \param HNO is the handle to the NodeObject
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:AddNodeObject
 * \see \fullref{fct:AddNodeObjectByName
 * \see \fullref{fct:AddNodeObjectSubtreeByName
 */
EXPORTDLL int RemoveNodeObject(HController HC, HNodeObjectCollection HNOC, HNodeObject HNO);

/**
 * Gets the count of NodeObjects in the NodeObjectCollection.
 *
 * \param HC is the handle to the controller
 * \param HNOC is the handle to the NodeObjectCollection
 * \param cnt holds the number of NodeObjects after returning successfully
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetNodeObjectAtIndex
 */
EXPORTDLL int GetNodeObjectCount(HController HC, HNodeObjectCollection HNOC, int *cnt);

/**
 * Gets the NodeObject at a given index in the NodeObjectCollection. 
 *
 * \param HC is the handle to the controller
 * \param HNOC is the handle to the NodeObjectCollection
 * \param index is the index of the wanted NodeObject where the first item has index~texttt0
 * \param HNO is the handle to the NodeObject
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetNodeObjectCount
 */
EXPORTDLL int GetNodeObjectAtIndex(HController HC, HNodeObjectCollection HNOC, int index, HNodeObject *HNO);

/**
 * Reads a complete NodeObjectCollection from the Controller. This is only necessary if NodeObjects are added by calls to \nameref{fct:AddNodeObjectByName} and the variable cache is disabled.
 *
 * \param HC is the handle to the controller
 * \param HNOC is the handle to the NodeObjectCollection
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *     E_NOEXIST if a node in the NodeObjectCollection does not exist
 *
 * \see \fullref{fct:AddNodeObjectByName
 * \see \fullref{fct:WriteNodeObjectCollection
 * \see \fullref{fct:WriteNodeObjectCollectionSync
 * \see \fullref{fct:EnableVariableCache
 */
EXPORTDLL int ReadNodeObjectCollection(HController HC, HNodeObjectCollection HNOC);

/**
 * Writes all NodeObjects in the collection in 1~step. This is much faster than writing each NodeObject in a seperate step. If changing these nodes forces other nodes to change then \nameref{fct:WriteNodeObjectCollectionSync} should be used.
 *
 * \param HC is the handle to the controller
 * \param HNOC is the handle to the NodeObjectCollection
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:WriteNodeObjectCollectionSync
 */
EXPORTDLL int WriteNodeObjectCollection(HController HC, HNodeObjectCollection HNOC);

/**
 * Writes all NodeObjects in the collection in 1~step. This is much faster than writing each NodeObject in a seperate step. This function waits until the firmware sents all updates. This is useful if setting a variable changes other variables.
 *
 * \param HC is the handle to the controller
 * \param HNOC is the handle to the NodeObjectCollection
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:WriteNodeObjectCollection
 */
EXPORTDLL int WriteNodeObjectCollectionSync(HController HC, HNodeObjectCollection HNOC);

/**
 * Gets information about jobs on the controller. It is guaranteed that this function always returns a valid pointer to a \nameref{sec:JobInfo}-structure. The returned information has to be destroyed with a call to \nameref{fct:DestroyJobInfo}.
 * \begin{lstlisting}[caption={GetJobInfo example}]
 * int found = 0;
 * ARG_JOBINFO *jobinfo = GetJobInfo(HC);
 * if (jobinfo->jobcount == 0 ) {
 *   printf("No jobs on the controller.\n");
 * } else {
 *   if ( jobinfo->jobcount == 1) 
 *     printf("%i job on the controller:\n", jobinfo->jobcount);
 *   else 
 *     printf("%i jobs on the controller:\n", jobinfo->jobcount);
 * 
 *   for (int i=0; i < jobinfo->jobcount; ++i) {
 *     if ( strcmp(jobinfo->jobnames[i], jobinfo->selectednode) == 0 ) {
 *       printf("\t*%i: %s\n", i, jobinfo->jobnames[i]);
 *       found = 1;
 *     } else 
 *       printf("\t %i: %s\n", i, jobinfo->jobnames[i]);
 *   }
 * 
 *   if ( !found && (strcmp(jobinfo->selectednode,"") != 0) ) 
 *     printf("\t Other node selected: %s\n", jobinfo->selectednode);
 * }
 * 
 * DestroyJobInfo(jobinfo);
 * \end{lstlisting}
 *
 * \param HC is the handle to the controller
 *
 * \return
 *     
 *
 * \see \fullref{fct:DestroyJobInfo
 * \see \fullref{sec:JobInfo
 */
EXPORTDLL ARG_JOBINFO* GetJobInfo(HController HC);

/**
 * Destroys the \nameref{sec:JobInfo}-structure which was obtained by a call to\newline 
 * \nameref{fct:GetJobInfo}.
 *
 * \param info is the namerefsec:JobInfo
 *
 * \see \fullref{fct:GetJobInfo
 * \see \fullref{sec:JobInfo
 */
EXPORTDLL void DestroyJobInfo(ARG_JOBINFO *info);

/**
 * \notice{This function is deprecated.}{\vspace{-1.5ex}}{\item[\textbullet] Use \nameref{fct:GetJobInfo} instead.}
 * Gets the strlen of all job names on the controller. To get the job names call \nameref{fct:GetJobNames}. 
 *
 * \param HC is the handle to the controller
 * \param len holds the strlen of the job names after returning succesfully
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetJobNames
 */
EXPORTDLL int GetJobNamesLen(HController HC, int *len);

/**
 * \notice{This function is deprecated.}{\vspace{-1.5ex}}{\item[\textbullet] Use \nameref{fct:GetJobInfo} instead.}
 * Gets the names of the jobs loaded on the controller. The job names are seperated by semicolons; e.g.\,\enquote{\texttt{Job1;Job2}}.
 *
 * \param HC is the handle to the controller
 * \param buffer is the pointer to the buffer for the job names
 * \param len is the size of the buffer
 *
 * \return
 *     E_OK on success
 *     E_NOSPACE if the buffer is too small
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetJobNamesLen
 */
EXPORTDLL int GetJobNames(HController HC, char *buffer, int len);

/**
 * Clears all Jobs from the controller.
 *
 * \param HC is the handle to the controller
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:LoadJob
 */
EXPORTDLL int JobClearAll(HController HC);

/**
 * Gets information about how many job node types are supported by the controller. Information about those types can be obtained with \nameref{fct:GetJobNodeTypeInfo}.
 * \begin{lstlisting}[caption={GetJobNodeTypesCount example}]
 * int cnt = 0;
 * ARG_JOBNODEINFO *jobnodeinfo = NULL;
 * if ( GetJobNodeTypesCount(HC, &cnt) != E_OK ) {
 *   printf("Failure getting the count of the JobNodeTypes\n");
 * } else {
 *   for ( int i=0; i<cnt; ++i) {
 *     jobnodeinfo = GetJobNodeTypeInfo(HC, i);
 *     if ( jobnodeinfo != NULL ) {
 *       printf("%s -\t\t ", jobnodeinfo->fullname);
 *       if ( jobnodeinfo->usercreatable ) 
 *         printf(" usercreatable");
 *       if ( jobnodeinfo->canmakelines )
 *         printf(" canmakelines");
 *       if ( jobnodeinfo->canpasslines ) 
 *         printf(" canpasslines");
 *       if ( jobnodeinfo->acceptssubnodes ) 
 *         printf(" acceptssubnodes");
 *       printf("\n");
 *     }
 *     DestroyJobNodeTypeInfo(jobnodeinfo);
 *   }
 * }
 * \end{lstlisting}
 *
 * \param HC is the handle to the controller
 * \param cnt holds the number of supported job node types after returning with textttE_OK
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetJobNodeTypeInfo
 * \see \fullref{fct:DestroyJobNodeTypeInfo
 * \see \fullref{sec:JobNodeInfo
 */
EXPORTDLL int GetJobNodeTypesCount(HController HC, int *cnt);

/**
 * Gets information about a specific job node type, see \nameref{sec:JobNodeInfo}. On error, \texttt{NULL} is returned. The obtained structure has to be destroyed with \nameref{fct:DestroyJobNodeTypeInfo}. For a complete example see \nameref{fct:GetJobNodeTypesCount}.
 *
 * \param HC is the handle to the controller
 * \param index is the index of the job node type
 *
 * \return
 *     
 *
 * \see \fullref{fct:GetJobNodeTypeInfo
 * \see \fullref{fct:DestroyJobNodeTypeInfo
 * \see \fullref{sec:JobNodeInfo
 */
EXPORTDLL ARG_JOBNODEINFO* GetJobNodeTypeInfo(HController HC, int index);

/**
 * Destroys a job node type info structure which was obtained with \nameref{fct:GetJobNodeTypeInfo}.
 *
 * \param info is the pointer to the job node type info structure (can be textttNULL
 *
 * \see \fullref{fct:GetJobNodeTypeInfo
 * \see \fullref{sec:JobNodeInfo
 */
EXPORTDLL void DestroyJobNodeTypeInfo(ARG_JOBNODEINFO *info);

/**
 * If calling GetJobLines then the firmware returns several packages of data. The \nameref{fct:JobLinesCallbackFunction} is called for each package. After the last package callback has returned this function returns.
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param doSubnodes If texttt1
 * \param userHandle is the handle set by the user in namerefsec:LineInfo
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:JobLinesCallbackFunction
 * \see \fullref{fct:GetJobLinesAbort
 * \see \fullref{fct:RegisterOnJobLines
 * \see \fullref{fct:UnregisterOnJobLines
 */
EXPORTDLL int GetJobLines(HController HC, HNodeObject HNO, int doSubnodes, DWORD userHandle);

/**
 * Aborts all calls to JobGetLines.
 *
 * \param HC is the handle to the controller
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:JobLinesCallbackFunction
 * \see \fullref{fct:RegisterOnJobLines
 * \see \fullref{fct:UnregisterOnJobLines
 */
EXPORTDLL int GetJobLinesAbort(HController HC);

/**
 * \todo{Hier fehlt die Beschreibung. JobNodesOffset}
 * \todo{Was sind dX und dY?}
 *
 * \param HC ist the handle to the controller
 * \param HNO is the handle to the root node of the offset
 * \param HNOC is the handle to the NodeObjectCollection
 * \param float dX
 * \param float dY
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:JobNodesScale
 * \see \fullref{fct:JobNodesRotate
 * \see \fullref{fct:JobNodesTransform
 */
EXPORTDLL int JobNodesOffset(HController HC, HNodeObject HNO, HNodeObjectCollection HNOC, float dX, float dY);

/**
 * \todo{Hier fehlt die Beschreibung. JobNodesScale}
 * 
 * \todo{scale?}
 * \todo{Was sind dX, dY, sX und sY?}
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the root node of the scale
 * \param HNOC is the handle to the NodeObjectCollection
 * \param float x0
 * \param float y0
 * \param float sX
 * \param float sY
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:JobNodesOffset
 * \see \fullref{fct:JobNodesRotate
 * \see \fullref{fct:JobNodesTransform
 */
EXPORTDLL int JobNodesScale(HController HC, HNodeObject HNO, HNodeObjectCollection HNOC, float x0, float y0, float sX, float sY);

/**
 * \todo{Hier fehlt die Beschreibung. JobNodesRotate}
 * 
 * \todo{rotate?}
 * \todo{Was sind cX, cY, dA?}
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the root node of the rotate
 * \param HNOC is the handle to the NodeObjectCollection
 * \param float cX
 * \param float cY
 * \param float dA
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:JobNodesOffset
 * \see \fullref{fct:JobNodesScale
 * \see \fullref{fct:JobNodesTransform
 */
EXPORTDLL int JobNodesRotate(HController HC, HNodeObject HNO, HNodeObjectCollection HNOC, float cX, float cY, float dA);

/**
 * \todo{Hier fehlt die Beschreibung. JobNodesTransform}
 * 
 * \todo{transform?}
 * \todo{Was sind v00 bis v12?}
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the root node of the transform
 * \param HNOC is the handle to a NodeObjectCollection
 * \param float v00
 * \param float v01
 * \param float v02
 * \param float v10
 * \param float v11
 * \param float v12
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:JobNodesOffset
 * \see \fullref{fct:JobNodesScale
 * \see \fullref{fct:JobNodesRotate
 */
EXPORTDLL int JobNodesTransform(HController HC, HNodeObject HNO, HNodeObjectCollection HNOC, float v00, float v01, float v02, float v10, float v11, float v12);

/**
 * Gets info for timed signal streams. The returned structure has to be freed using \nameref{fct:DestroyTssInfo}.
 * \begin{lstlisting}[caption={GetTssInfo example}]
 * ARG_TSS_INFO *info = GetTssInfo(HC);
 * if ( info ) {
 *   if ( info->channelcount > 0 ) {
 *     for (int i=0; i<info->channelcount; ++i) {
 *       ARG_TSS_CHANNELTYPE *type = info->channeltype[i];
 *       if ( type ) {
 *         printf("\nChanneltype %d\n",(i+1));
 *         printf("=============\n");
 *         printf("  Name:                    %s\n", type->name);
 *         printf("  GetCoordinateSystemName: %s\n", type->coordname);
 *         if ( type->axiscount == 0 ) {
 *           printf("  No Axisnames found\n");
 *         } else {
 *           for (int j=0; j<type->axiscount; ++j) {
 *             printf("    Axis %2d: %s\n", j, type->axisname[j]);
 *           }
 *         }
 *       }
 *     }
 *   } else {
 *     printf("No TimedSignalStream-Types found\n");
 *   }
 * 
 *   DestroyTssInfo(info);
 * }
 * \end{lstlisting}
 *
 * \param HC is the handle to the controller
 *
 * \return
 *     
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:DestroyTssInfo
 */
EXPORTDLL ARG_TSS_INFO* GetTssInfo(HController HC);

/**
 * Frees a structure that was obtained by \nameref{fct:GetTssInfo}. For an example see \nameref{fct:GetTssInfo}.
 *
 * \param HC is the handle to the controller
 *
 * \return
 *     
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetTssInfo
 * \see \fullref{fct:RequestTssDataConnection
 * \see \fullref{fct:CancelTssDataConnection
 */
EXPORTDLL void DestroyTssInfo(ARG_TSS_INFO *info);

/**
 * \todo{Hier fehlt die Beschreibung. RequestTssDataConnection}
 *
 * \param HC is the handle to the controller
 * \param channeltype is the channel type (name field of ARG_TSS_CHANNELTYPE)
 * \param specifier depends on the channel type; textttNULL
 * \param id holds the connection textttID
 *
 * \return
 *     E_OK if the data connection could be established
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetTssInfo
 * \see \fullref{fct:CancelTssDataConnection
 */
EXPORTDLL int RequestTssDataConnection(HController HC, const char *channeltype, const char *specifier, int *id);

/**
 *
 * \param HC is the handle to the controller
 * \param id is the textttID
 *
 * \return
 *     E_OK if the data connection can be cancelled
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetTssInfo
 * \see \fullref{fct:CancelTssDataConnection
 */
EXPORTDLL int CancelTssDataConnection(HController HC, int id);

/**
 * Creates a special job on the controller. 
 * \begin{lstlisting}[caption={BeginSpecialJob example}]
 * HNodeObject HNO;
 * if ( BeginSpecialJob(HC, "Pincushion", 1, &HNO) == E_OK ) {
 *   assert(HNO != ARG_INVALID_HANDLE_VALUE);
 *   //...
 *   EndSpecialJob(HC);
 * }
 * \end{lstlisting}
 * \tip{This function is for internal use only.}
 *
 * \param HC is the handle to the controller
 * \param name is the name of the special job
 * \param verify texttt1
 * \param HNO holds the HNodeObject of the created job after returning with textttE_OK
 *
 * \return
 *     E_OK if the special job has been created
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:EndSpecialJob
 */
EXPORTDLL int BeginSpecialJob(HController HC, const char *name, int verify, HNodeObject *HNO);

/**
 * \todo{Hier fehlt die Beschreibung. EndSpecialJob}
 * \tip{This function is for internal use only.}
 *
 * \param HC is the handle to the controller
 *
 * \return
 *     E_OK If the last special job has been ended with this call
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:BeginSpecialJob
 */
EXPORTDLL int EndSpecialJob(HController HC);

/**
 * Gets information about the drivers which are available with the firmware in use on the controller. 
 * \begin{lstlisting}[caption={GetDriverInfo example}]
 * ARG_DRIVERINFO *info = GetDriverInfo(HC);
 * if ( info != NULL ) {
 *   printf("Found %i drivers\n", info->drivercount);
 *   for (int i=0; i<info->drivercount; ++i) {
 *     printf("Driver: %s (Version: %s)\n", info->driver[i]->name, info->driver[i]->version);
 *     printf("Vendor: %s\n", info->driver[i]->vendor);
 *     printf("Comment: %s\n", info->driver[i]->comment);
 *     printf("\n");
 *   }
 *   DestroyDriverInfo(info);
 * }
 * \end{lstlisting}
 * information about available drivers on the controller}
 *
 * \param HC is the handle to the controller
 *
 * \return
 *     
 *
 * \see \fullref{fct:DestroyDriverInfo
 * \see \fullref{sec:DriverInfo
 * \see \fullref{sec:SingleDriverInfo
 */
EXPORTDLL ARG_DRIVERINFO* GetDriverInfo(HController HC);

/**
 * Destroys the structure obtained by \nameref{fct:GetDriverInfo}.
 *
 * \param info the pointer to the namerefsec:DriverInfo
 *
 * \see \fullref{fct:GetDriverInfo
 */
EXPORTDLL void DestroyDriverInfo(ARG_DRIVERINFO *info);

/**
 * Creates a device from a driver.
 * \begin{lstlisting}[caption={CreateDevice example}]
 * if ( CreateDevice(HC, "IPG YLP", "My Laser") != E_OK ) {
 *   printf("Could not create device.");
 * }
 * \end{lstlisting}
 *
 * \param HC is the handle to the controller
 * \param drivername is the name of the driver
 * \param devicename is the name of the device. This name must be unique in the firm-ware
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetDriverInfo
 * \see \fullref{fct:GetAllDeviceInfo
 * \see \fullref{fct:ActivateDevice
 */
EXPORTDLL int CreateDevice(HController HC, const char *drivername, const char *devicename);

/**
 * Returns the handle to the given device.
 *
 * \param HC is the handle to the controller
 * \param deviceName is the name of the device
 *
 * \return
 *     the handle of the device
 *     ARG_INVALID_HANDLE_VALUE if the device was not found
 *
 * \see \fullref{fct:CreateDevice
 * \see \fullref{fct:GetAllDeviceInfo
 */
EXPORTDLL HDevice GetDevice(HController HC, const char *deviceName);

/**
 * Deletes the given device. The device is identified by its handle.
 * \begin{lstlisting}[caption={DeleteDevice example}]
 * HDevice HD = GetDevice(HC, "My Laser");
 * if ( HD != ARG_INVALID_HANDLE_VALUE ) {
 *   DeleteDevice(HC, HD);
 * }
 * \end{lstlisting}
 *
 * \param HC is the handle to the controller
 * \param HD is the handle to the device
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:DeleteDeviceByName
 * \see \fullref{fct:CreateDevice
 * \see \fullref{fct:GetAllDeviceInfo
 */
EXPORTDLL int DeleteDevice(HController HC, HDevice HD);

/**
 * Deletes the given device. The device is identified by its name.
 * \begin{lstlisting}[caption={DeleteDeviceByName example}]
 * DeleteDeviceByName(HC, "My Laser");
 * \end{lstlisting}
 *
 * \param HC is the handle to the controller
 * \param devicename is the name of the device
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:DeleteDeviceByName
 * \see \fullref{fct:CreateDevice
 * \see \fullref{fct:GetAllDeviceInfo
 */
EXPORTDLL int DeleteDeviceByName(HController HC, const char *devicename);

/**
 * Returns a structure with information about all devices used on the controller. All strings in the structure are valid, so there is no need to test for \texttt{NULL}, but can contain emtpy strings (\enquote{}).
 * \begin{lstlisting}[caption={GetAllDeviceInfo example}]
 * ARG_DEVICEINFO *info = GetAllDeviceInfo(HC);
 * if ( info ) {
 *   for (int i=0; i<info->devicecount; ++i) {
 *     ARG_SINGLEDEVICEINFO *singleinfo = device[i];
 *     printf("Device: %s\n");
 *   }
 *   DestroyDeviceInfo(info);
 * }
 * \end{lstlisting}
 * \tip{The returned structure has to be destroyed by using \nameref{fct:DestroyDeviceInfo}.}
 * information about the devices on the controller}
 *
 * \param HC is the handle to the controller
 *
 * \return
 *     the pointer to the ARG_DEVICEINFO-structure with
 *
 * \see \fullref{fct:DestroyDeviceInfo
 * \see \fullref{fct:GetDeviceInfo
 * \see \fullref{sec:DeviceInfo
 * \see \fullref{sec:SingleDeviceInfo
 */
EXPORTDLL ARG_DEVICEINFO* GetAllDeviceInfo(HController HC);

/**
 * Destroys the structure which was obtained by a call to 
 * \nameref{fct:GetAllDeviceInfo}. All internally used memory
 * will be destroyed.
 *
 * \param info is the pointer to the ARG_DEVICEINFO-structure
 *
 * \see \fullref{fct:GetAllDeviceInfo
 */
EXPORTDLL void DestroyDeviceInfo(ARG_DEVICEINFO *info);

/**
 * Returns information about a particular device on the controller. All strings in the structure are valid, so there is no need to test for \texttt{NULL}, but can contain emtpy strings (\enquote{}).
 * \begin{lstlisting}[caption={GetDeviceInfo example}]
 * HDevice HD = GetDevice(HC,"My Laser");
 * if ( HD != ARG_INVALID_HANDLE_VALUE ) {
 *   ARG_SINGLEDEVICEINFO *info = GetDeviceInfo(HC, HD);
 *   if ( info ) {
 *     printf("Devicename: %s\n", info->name);
 *     DestroySingleDeviceInfo(info);
 *   }
 * }
 * \end{lstlisting}
 * \tip{The returned structure has to be destroyed by using \nameref{fct:DestroySingleDeviceInfo}.}
 * information about the devices on the controller}
 *
 * \param HC is the handle to the controller
 * \param HD is the handle to the device
 *
 * \return
 *     the pointer to the ARG_SINGLEDEVICEINFO-structure with
 *
 * \see \fullref{fct:DestroySingleDeviceInfo
 * \see \fullref{fct:GetAllDeviceInfo
 * \see \fullref{sec:DeviceInfo
 * \see \fullref{sec:SingleDeviceInfo
 */
EXPORTDLL ARG_SINGLEDEVICEINFO* GetDeviceInfo(HController HC, HDevice HD);

/**
 * Destroys the structure obtained by \nameref{fct:GetDeviceInfo}.
 *
 * \param info is the pointer to the ARG_SINGLEDEVICEINFO-structure
 *
 * \see \fullref{fct:GetDeviceInfo
 */
EXPORTDLL void DestroySingleDeviceInfo(ARG_SINGLEDEVICEINFO *info);

/**
 * Activates the given device. When this function returns, this does not mean that the device is active. The state of activation can be monitored by using \nameref{fct:RegisterOnDeviceStateChanged}.
 * \todo{Der Ausdruck "activation" ist mir suspect. Gibt es einen Überbegriff?}
 *
 * \param HC is the handle to the controller
 * \param HD is the handle to the device
 *
 * \return
 *     E_OK on success
 *     E_INVALID if the device is already active
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnDeviceStateChanged
 * \see \fullref{fct:GetAllDeviceInfo
 * \see \fullref{fct:GetDeviceInfo
 * \see \fullref{fct:DeactivateDevice
 */
EXPORTDLL int ActivateDevice(HController HC, HDevice HD);

/**
 * Activates the given device. When this function returns, this does not mean that the device is active. The state of the activation can be monitored by using \nameref{fct:RegisterOnDeviceStateChanged}.
 * \todo{Der Ausdruck "activation" ist mir suspect. Gibt es einen Überbegriff?}
 *
 * \param HC is the handle to the controller
 * \param devicename is the name of the device that shall be activated
 *
 * \return
 *     E_OK on success
 *     E_INVALID if the device is already active
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnDeviceStateChanged
 * \see \fullref{fct:GetAllDeviceInfo
 * \see \fullref{fct:GetDeviceInfo
 * \see \fullref{fct:DeactivateDevice
 */
EXPORTDLL int ActivateDeviceByName(HController HC, const char *devicename);

/**
 * Deactivates the given device. When this function returns, this does not mean that the device is inactive. The state of the deactivation can be monitored by using \nameref{fct:RegisterOnDeviceStateChanged}.
 * \todo{Der Ausdruck "deactivation" ist mir suspect. Gibt es einen Überbegriff?}
 *
 * \param HC is the handle to the controller
 * \param HD is the handle to the device
 *
 * \return
 *     E_OK on success
 *     E_INVALID if the device is already inactive
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnDeviceStateChanged
 * \see \fullref{fct:GetAllDeviceInfo
 * \see \fullref{fct:GetDeviceInfo
 * \see \fullref{fct:ActivateDevice
 */
EXPORTDLL int DeactivateDevice(HController HC, HDevice HD);

/**
 * Deactivates the given device. When this function returns, this does not mean that the device is inactive. The state of the deactivation can be monitored by using \nameref{fct:RegisterOnDeviceStateChanged}.
 * \todo{Der Ausdruck "deactivation" ist mir suspect. Gibt es einen Überbegriff?}
 *
 * \param HC is the handle to the controller
 * \param devicename is the name of the device that shall be deactivated
 *
 * \return
 *     E_OK on success
 *     E_INVALID if the device is already inactive
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnDeviceStateChanged
 * \see \fullref{fct:GetAllDeviceInfo
 * \see \fullref{fct:GetDeviceInfo
 * \see \fullref{fct:ActivateDevice
 */
EXPORTDLL int DeactivateDeviceByName(HController HC, const char *devicename);

/**
 * Sets the device to managed. The device is identified by its handle.
 * \todo{Der Anwender fragt sich: Was bedeutet "managed"?}
 *
 * \param HC is the handle to the controller
 * \param HD is the handle to the device
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetAllDeviceInfo
 * \see \fullref{fct:GetDeviceInfo
 * \see \fullref{fct:SetManagedByName
 * \see \fullref{fct:SetUnmanaged
 * \see \fullref{fct:SetUnmanagedByName
 * \see \fullref{fct:IsManaged
 * \see \fullref{fct:IsManagedByName
 */
EXPORTDLL int SetManaged(HController HC, HDevice HD);

/**
 * Sets the device to managed. The device is identified by its name.
 * \todo{Der Anwender fragt sich: Was bedeutet "managed"?}
 *
 * \param HC is the handle to the controller
 * \param devicename is the name of the device
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetAllDeviceInfo
 * \see \fullref{fct:GetDeviceInfo
 * \see \fullref{fct:SetManaged
 * \see \fullref{fct:SetUnmanaged
 * \see \fullref{fct:SetUnmanagedByName
 * \see \fullref{fct:IsManaged
 * \see \fullref{fct:IsManagedByName
 */
EXPORTDLL int SetManagedByName(HController HC, const char *devicename);

/**
 * Sets the device to unmanaged. The device is identified by its handle.
 * \todo{Der Anwender fragt sich: Was bedeutet "unmanaged"?}
 *
 * \param HC is the handle to the controller
 * \param HD is the handle to the device
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetAllDeviceInfo
 * \see \fullref{fct:GetDeviceInfo
 * \see \fullref{fct:SetUnmanagedByName
 * \see \fullref{fct:SetManaged
 * \see \fullref{fct:SetManagedByName
 * \see \fullref{fct:IsManaged
 * \see \fullref{fct:IsManagedByName
 */
EXPORTDLL int SetUnmanaged(HController HC, HDevice HD);

/**
 * Set the device to unmanaged. The device is identified by its name.
 * \todo{Der Anwender fragt sich: Was bedeutet "unmanaged"?}
 *
 * \param HC is the handle to the controller
 * \param devicename is the name of the device
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetAllDeviceInfo
 * \see \fullref{fct:GetDeviceInfo
 * \see \fullref{fct:SetUnmanaged
 * \see \fullref{fct:SetManaged
 * \see \fullref{fct:SetManagedByName
 * \see \fullref{fct:IsManaged
 * \see \fullref{fct:IsManagedByName
 */
EXPORTDLL int SetUnmanagedByName(HController HC, const char *devicename);

/**
 * Returns in variable \texttt{managed} whether the device is managed or not. The device is identified by its handle.
 * \todo{Der Anwender fragt sich: Was bedeutet "managed or not"?}
 * \todo{Aussage unklar; Und warum überhaupt bool? Sonst wird doch auch 0 und 1 verwendet.}
 *
 * \param HC is the handle to the controller
 * \param HD is the handle to the device
 * \param managed holds texttttrue
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetAllDeviceInfo
 * \see \fullref{fct:GetDeviceInfo
 * \see \fullref{fct:IsManagedByName
 * \see \fullref{fct:SetManaged
 * \see \fullref{fct:SetManagedByName
 * \see \fullref{fct:SetUnmanaged
 * \see \fullref{fct:SetUnmanagedByName
 */
EXPORTDLL int IsManaged(HController HC, HDevice HD, bool &managed);

/**
 * Returns in the variable \textbf{managed} whether the device is managed or not. The device is identified by its name.
 * \todo{Was bedeutet "managed or not"?}
 * \todo{Worauf bezieht sich E_OK? true und/oder false? Und warum überhaupt bool? Sonst wird doch auch 0 und 1 verwendet.}
 *
 * \param HC is the handle to the controller
 * \param devicename is the name of the device
 * \param managed If the function returns with E_OK: texttttrue
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetAllDeviceInfo
 * \see \fullref{fct:GetDeviceInfo
 * \see \fullref{fct:IsManaged
 * \see \fullref{fct:SetManaged
 * \see \fullref{fct:SetManagedByName
 * \see \fullref{fct:SetUnmanaged
 * \see \fullref{fct:SetUnmanagedByName
 */
EXPORTDLL int IsManagedByName(HController HC, const char *devicename, bool &managed);

/**
 * Returns, whether it is possible to reset the device or not. The device is identified by its handle.
 *
 * \param HC is the handle to the controller
 * \param HD is the handle to the device
 *
 * \return
 *     E_OK if the device can be reset
 *     E_NALLOWED if the device can not be reset
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetAllDeviceInfo
 * \see \fullref{fct:GetDeviceInfo
 * \see \fullref{fct:IsDeviceResettableByName
 * \see \fullref{fct:ResetDevice
 */
EXPORTDLL int IsDeviceResettable(HController HC, HDevice HD);

/**
 * Returns, whether it is possible to reset the device or not. The device is identified by its name.
 *
 * \param HC is the handle to the controller
 * \param devicename is the name of the device
 *
 * \return
 *     E_OK if the device can be reset
 *     E_NALLOWED if the device can not be reset
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetAllDeviceInfo
 * \see \fullref{fct:GetDeviceInfo
 * \see \fullref{fct:IsDeviceResettable
 * \see \fullref{fct:ResetDeviceByName
 */
EXPORTDLL int IsDeviceResettableByName(HController HC, const char *devicename);

/**
 * Resets the given device. The device is identified by its handle. When this function returns, this does not mean that the device has been reset. The state of the reset can be monitored by using \nameref{fct:RegisterOnDeviceStateChanged}.
 * \todo{Der Ausdruck "state of the reset" ist suspekt. --> device state?}
 *
 * \param HC is the handle to the controller
 * \param HD is the handle to the device
 *
 * \return
 *     E_OK on success
 *     E_NALLOWED if the device can not be reset
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnDeviceStateChanged
 * \see \fullref{fct:GetAllDeviceInfo
 * \see \fullref{fct:GetDeviceInfo
 * \see \fullref{fct:IsDeviceResettable
 * \see \fullref{fct:ResetDeviceByName
 */
EXPORTDLL int ResetDevice(HController HC, HDevice HD);

/**
 * Resets the given device. The device is identified by its name. When this function returns, this does not mean that the device has been reset. The state of the reset can be monitored by using \nameref{fct:RegisterOnDeviceStateChanged}.
 * \todo{Der Ausdruck "state of the reset" ist suspekt. --> device state?}
 *
 * \param HC is the handle to the controller
 * \param devicename is the name of the device
 *
 * \return
 *     E_OK on success
 *     E_NALLOWED if the device can not be reset
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnDeviceStateChanged
 * \see \fullref{fct:GetAllDeviceInfo
 * \see \fullref{fct:GetDeviceInfo
 * \see \fullref{fct:IsDeviceResettable
 * \see \fullref{fct:ResetDevice
 */
EXPORTDLL int ResetDeviceByName(HController HC, const char *devicename);

/**
 * Creates a pen variable for a device.
 *
 * \param HC is the handle to the controller
 * \param drivername is the name of the driver; e.g.~textttLINEPAR
 * \param pennamevariable is the path to the pen variable; e.g. newline textttusr.pens.MyPen.linepar.common.speed_m
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetDriverInfo
 */
EXPORTDLL int CreatePenVar(HController HC, const char *drivername, const char *pennamevariable);

/**
 * Registers a callback for a NodeModified-event. Each time the given variable
 * changes its internal values (value, flags, index, \dots ) this function gets
 * called. Please note, that this function is partially redundant with the following
 * functions:\\
 * \fullref{fct:RegisterOnValueChangedExt}, 
 * \fullref{fct:RegisterOnValueChanged}, \newline
 * \fullref{fct:RegisterOnFlagsChangedExt},
 * \fullref{fct:RegisterOnFlagsChanged}
 * \tip{It is possible to have more than 1~callback for each variable.}
 *
 * \param HC is the handle to the controller
 * \param varname is the name of the variable; e.g.,enquotetextttstat.time.TimeStr
 * \param callback is the callback function
 * \param userpointer this pointer is given as parameter in the callback function
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:NodeModifiedCallbackFunction
 * \see \fullref{fct:UnregisterOnNodeModified
 * \see \fullref{fct:UnregisterOnNodeModifiedSingle
 * \see \fullref{fct:RegisterOnValueChangedExt
 * \see \fullref{fct:RegisterOnValueChanged
 * \see \fullref{fct:RegisterOnFlagsChangedExt
 * \see \fullref{fct:RegisterOnFlagsChanged
 * \see \fullref{fct:RegisterOnNameChangedExt
 * \see \fullref{fct:RegisterOnNameChanged
 * \see \fullref{fct:RegisterOnNodeDeletedExt
 * \see \fullref{fct:RegisterOnNodeDeleted
 * \see \fullref{fct:AddNodeObjectSubtreeByName
 */
EXPORTDLL int RegisterOnNodeModified(HController HC, const char *varname, NodeModifiedCallbackFunction callback, void *userpointer);

/**
 * Unregisters all callbacks of a NodeModified-event from the given variable.
 *
 * \param HC is the handle to the controller
 * \param varname is the name of the variable; e.g.,enquotetextttstat.time.TimeStr
 *
 * \return
 *     E_OK on success
 *     E_NOEXIST if a callback for the given variable has not been registered
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnNodeModified
 * \see \fullref{fct:UnregisterOnNodeModifiedSingle
 */
EXPORTDLL int UnregisterOnNodeModified(HController HC, const char *varname);

/**
 * Unregisters the given callback of a NodeModified-event from the given variable.
 *
 * \param HC is the handle to the controller
 * \param varname is the name of the variable; e.g.,enquotetextttstat.time.TimeStr
 * \param callback is the callback that shall be unregistered
 *
 * \return
 *     E_OK on success
 *     E_NOEXIST if the callback for that variable has not been registered
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:UnregisterOnNodeModified
 * \see \fullref{fct:RegisterOnNodeModified
 */
EXPORTDLL int UnregisterOnNodeModifiedSingle(HController HC, const char *varname, NodeModifiedCallbackFunction callback);

/**
 *
 * \param HC is the handle to the controller
 * \param callback is the callback function
 * \param userpointer this pointer is given as parameter in the callback function
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:UnregisterOnNodeModifiedGlobal
 */
EXPORTDLL int RegisterOnNodeModifiedGlobal(HController HC, NodeModifiedCallbackFunction callback, void *userpointer);

/**
 *
 * \param HC is the handle to the controller
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnNodeModifiedGlobal
 */
EXPORTDLL int UnregisterOnNodeModifiedGlobal(HController HC);

/**
 * Registers a callback for a ValueChange-event. Each time the given variable
 * changes the callback gets called with a NodeObject holding the
 * current value. After registering the callback is called initially with 
 * the current value of the node.
 * \tip{It is possible to have more than 1~callback for each variable.}
 *
 * \param HC is the handle to the controller
 * \param varname is the name of the variable; e.g.,enquotetextttstat.time.TimeStr
 * \param callback is the callback function
 * \param userpointer this pointer is given as parameter in the callback function
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:ValueChangeCallbackFunctionExt
 * \see \fullref{fct:UnregisterOnValueChanged
 * \see \fullref{fct:UnregisterOnValueChangedSingleExt
 * \see \fullref{fct:RegisterOnValueChanged
 * \see \fullref{fct:RegisterOnFlagsChangedExt
 * \see \fullref{fct:RegisterOnNameChangedExt
 * \see \fullref{fct:RegisterOnNodeDeletedExt
 * \see \fullref{fct:AddNodeObjectSubtreeByName
 */
EXPORTDLL int RegisterOnValueChangedExt(HController HC, const char *varname, ValueChangeCallbackFunctionExt callback, void *userpointer);

/**
 * Registers a callback for a ValueChange-event. Each time a variable
 * changes the callback gets called with a NodeObject holding the
 * current value. 
 * \tip{Only 1~global callback per controller is allowed at a time.}
 *
 * \param HC is the handle to the controller
 * \param callback is the callback function
 * \param userpointer this pointer is given as parameter in the callback function
 *
 * \return
 *     E_OK on success
 *     E_EXIST if already 1~global ValueChange callback was registered
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:ValueChangeCallbackFunctionExt
 * \see \fullref{fct:UnregisterOnValueChangedGlobal
 * \see \fullref{fct:UnregisterOnValueChanged
 * \see \fullref{fct:UnregisterOnValueChangedSingleExt
 * \see \fullref{fct:RegisterOnValueChanged
 * \see \fullref{fct:RegisterOnFlagsChangedExt
 * \see \fullref{fct:RegisterOnNameChangedExt
 * \see \fullref{fct:RegisterOnNodeDeletedExt
 * \see \fullref{fct:AddNodeObjectSubtreeByName
 */
EXPORTDLL int RegisterOnValueChangedGlobal(HController HC, ValueChangeCallbackFunctionExt callback, void *userpointer);

/**
 * Unregisters the global callback of a ValueChange-event from the given controller.
 *
 * \param HC is the handle to the controller
 *
 * \return
 *     E_OK on success
 *     E_NOEXIST if a callback for that variable was not registered
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:UnregisterOnValueChanged
 * \see \fullref{fct:RegisterOnValueChangedGlobal
 */
EXPORTDLL int UnregisterOnValueChangedGlobal(HController HC);

/**
 * Unregister a callback of a ValueChange-event from the given variable.
 *
 * \param HC is the handle to the controller
 * \param varname name of the variable; e.g.,enquotetextttstat.time.TimeStr
 * \param callback the callback to be unregistered
 *
 * \return
 *     E_OK on success
 *     E_NOEXIST if the callback for that variable was not registered
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:UnregisterOnValueChanged
 * \see \fullref{fct:RegisterOnValueChangedExt
 */
EXPORTDLL int UnregisterOnValueChangedSingleExt(HController HC, const char *varname, ValueChangeCallbackFunctionExt callback);

/**
 * Registers a callback for a ValueChange-event. Each time the given variable
 * changes the callback gets called with a NodeObject holding the
 * current value. After registration the callback is called initially with 
 * the current value of the node.
 * \tip{It is possible to have more than 1~callback for each variable.}
 *
 * \param HC is the handle to the controller
 * \param varname is the name of the variable; e.g.,enquotetextttstat.time.TimeStr
 * \param callback is the callback function
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:ValueChangeCallbackFunction
 * \see \fullref{fct:RegisterOnValueChangedExt
 * \see \fullref{fct:UnregisterOnValueChanged
 * \see \fullref{fct:UnregisterOnValueChangedSingle
 * \see \fullref{fct:RegisterOnFlagsChanged
 * \see \fullref{fct:RegisterOnNameChanged
 * \see \fullref{fct:RegisterOnNodeDeleted
 * \see \fullref{fct:AddNodeObjectSubtreeByName
 */
EXPORTDLL int RegisterOnValueChanged(HController HC, const char *varname, ValueChangeCallbackFunction callback);

/**
 * Unregisters all callbacks of a ValueChange-event from the given variable.
 *
 * \param HC is the handle to the controller
 * \param varname is the name of the variable; e.g.~enquotetextttstat.time.TimeStr
 *
 * \return
 *     E_OK on success
 *     E_NOEXIST if a callback for that variable has not been registered
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:UnregisterOnValueChangedSingle
 * \see \fullref{fct:RegisterOnValueChanged
 */
EXPORTDLL int UnregisterOnValueChanged(HController HC, const char *varname);

/**
 * Unregisters a callback of a ValueChange-event from the given variable.
 *
 * \param HC is the handle to the controller
 * \param varname is the name of the variable; e.g.,enquotetextttstat.time.TimeStr
 * \param callback is the callback that shall be unregistered
 *
 * \return
 *     E_OK on success
 *     E_NOEXIST if the callback for that variable has not been registered
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:UnregisterOnValueChanged
 * \see \fullref{fct:RegisterOnValueChanged
 */
EXPORTDLL int UnregisterOnValueChangedSingle(HController HC, const char *varname, ValueChangeCallbackFunction callback);

/**
 * Registers a callback for a FlagsChange-event. Each time the flags for the given 
 * variable change the callback gets called with a NodeObject holding the
 * current value. After registration the callback is called initially with the current 
 * value of the flags.
 * \tip{It is possible to have more than 1~callback for each variable.}
 *
 * \param HC is the handle to the controller
 * \param varname is the name of the variable; e.g.,enquotetextttstat.time.TimeStr
 * \param callback is the callback function
 * \param userpointer this pointer is given as parameter in the callback function
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:FlagsChangeCallbackFunctionExt
 * \see \fullref{fct:RegisterOnFlagsChanged
 * \see \fullref{fct:RegisterOnValueChangedExt
 * \see \fullref{fct:UnregisterOnFlagsChanged
 * \see \fullref{fct:UnregisterOnFlagsChangedSingleExt
 */
EXPORTDLL int RegisterOnFlagsChangedExt(HController HC, const char *varname, FlagsChangeCallbackFunctionExt callback, void *userpointer);

/**
 * Unregisters a callback of a FlagsChange-event.
 *
 * \param HC is the handle to the controller
 * \param varname name of the variable; e.g.,enquotetextttstat.time.TimeStr
 * \param callback is the callback function
 *
 * \return
 *     E_OK on success
 *     E_NOEXIST if the callback for that variable was not registered
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnFlagsChangedExt
 * \see \fullref{fct:RegisterOnFlagsChanged
 * \see \fullref{fct:UnregisterOnFlagsChanged
 */
EXPORTDLL int UnregisterOnFlagsChangedSingleExt(HController HC, const char *varname, FlagsChangeCallbackFunctionExt callback);

/**
 * Registers a callback of a FlagsChange-event.
 *
 * \param HC is the handle to the controller
 * \param callback is the callback function
 * \param userpointer this pointer is given as parameter in the callback function
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:UnregisterOnFlagsChangedGlobal
 */
EXPORTDLL int RegisterOnFlagsChangedGlobal(HController HC, FlagsChangeCallbackFunctionExt callback, void *userpointer);

/**
 * Unregisters a callback of a FlagsChange-event.
 *
 * \param HC is the handle to the controller
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnFlagsChangedGlobal
 */
EXPORTDLL int UnregisterOnFlagsChangedGlobal(HController HC);

/**
 * Registers a callback for a FlagsChange-event. Each time the flags for the given 
 * variable change, the callback gets called with a NodeObject holding the
 * current value. After registration the callback is called initially with the current value of the
 * flags.
 * \tip{It is possible to have more than 1~callback for each variable.}
 *
 * \param HC is the handle to the controller
 * \param varname is the name of the variable; e.g.~enquotetextttstat.time.TimeStr
 * \param callback is the the callback function
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:FlagsChangeCallbackFunction
 * \see \fullref{fct:RegisterOnFlagsChangedExt
 * \see \fullref{fct:RegisterOnValueChanged
 * \see \fullref{fct:UnregisterOnFlagsChanged
 * \see \fullref{fct:UnregisterOnFlagsChangedSingle
 */
EXPORTDLL int RegisterOnFlagsChanged(HController HC, const char *varname,FlagsChangeCallbackFunction callback);

/**
 * Unregisters all callbacks of a FlagsChange-event.
 *
 * \param HC is the handle to the controller
 * \param varname is the name of the variable; e.g.~enquotetextttstat.time.TimeStr
 *
 * \return
 *     E_OK on success
 *     E_NOEXIST if a callback for that variable has not been registered
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnFlagsChanged
 * \see \fullref{fct:RegisterOnFlagsChangedExt
 * \see \fullref{fct:UnregisterOnFlagsChangedSingle
 */
EXPORTDLL int UnregisterOnFlagsChanged(HController HC, const char *varname);

/**
 * Unregisters a callback of a FlagsChange-event.
 *
 * \param HC is the handle to the controller
 * \param varname is the name of the variable; e.g.~enquotetextttstat.time.TimeStr
 * \param callback is the callback function
 *
 * \return
 *     E_OK on success
 *     E_NOEXIST if the callback for that variable has not been registered
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnFlagsChanged
 * \see \fullref{fct:UnregisterOnFlagsChanged
 */
EXPORTDLL int UnregisterOnFlagsChangedSingle(HController HC, const char *varname, FlagsChangeCallbackFunction callback);

/**
 * Registers a callback for a NodeMoved-event. Each time a node is moved on the
 * controller the callback function gets called.
 * \tip{It is possible to have more than 1~callback at a time.}
 *
 * \param HC is the handle to the controller
 * \param callback is the callback that gets called when a node is moved on the controller
 * \param userpointer This pointer is passed as a parameter to the callback function
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:NodeMovedCallbackFunction
 * \see \fullref{fct:UnregisterOnNodeMoved
 * \see \fullref{fct:UnregisterOnNodeMovedSingle
 */
EXPORTDLL int RegisterOnNodeMoved(HController HC, NodeMovedCallbackFunction callback, void *userpointer);

/**
 * Unregisters all callbacks of a NodeMoved-event.
 *
 * \param HC is the handle to the controller
 *
 * \return
 *     E_OK on success
 *     E_NOEXIST if a callback has not been registered
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnNodeMoved
 * \see \fullref{fct:UnregisterOnNodeMovedSingle
 */
EXPORTDLL int UnregisterOnNodeMoved(HController HC);

/**
 * Registers a callback for a NodeMoved-event. Each time a node is moved on the
 * controller this callback function gets called.
 * \tip{It is possible to have more than 1~callback at a time.}
 * controller}
 *
 * \param HC is the handle to the controller
 * \param callback is the callback that gets called when a node is moved on the
 * \param userpointer This pointer is passed as a parameter to the callback function
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:NodeMovedCallbackFunction
 * \see \fullref{fct:UnregisterOnNodeMoved
 * \see \fullref{fct:UnregisterOnNodeMovedSingle
 */
EXPORTDLL int RegisterOnNodeMovedExt(HController HC, NodeMovedCallbackFunctionExt callback, void *userpointer);

/**
 * Unregisters a callback for a NodeMoved-event.
 *
 * \param HC is the handle to the controller
 * \param callback is the callback function
 *
 * \return
 *     E_OK on success
 *     E_NOEXIST if the callback has not been registered
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnNodeMoved
 * \see \fullref{fct:UnregisterOnNodeMoved
 */
EXPORTDLL int UnregisterOnNodeMovedSingle(HController HC, NodeMovedCallbackFunction callback);

/**
 * Unregisters a callback for a NodeMoved-event.
 *
 * \param HC is the handle to the controller
 * \param callback is the callback function
 *
 * \return
 *     E_OK on success
 *     E_NOEXIST if the callback has not been registered
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnNodeMoved
 * \see \fullref{fct:UnregisterOnNodeMoved
 */
EXPORTDLL int UnregisterOnNodeMovedSingleExt(HController HC, NodeMovedCallbackFunctionExt callback);

/**
 * Registers a callback for NodeCreate-events. This callback gets called each
 * time a new node has been created on the controller.
 * \tip{It is possible to have more than 1~callback at a time.}
 *
 * \param HC is the handle to the controller
 * \param callback is the callback function
 * \param userpointer This pointer is passed as a parameter to the callback function
 *
 * \return
 *     E_OK on success
 *     E_EXIST if the callback has already been registered
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:NodeCreatedCallbackFunctionExt
 * \see \fullref{fct:RegisterOnNodeCreated
 * \see \fullref{fct:UnregisterOnNodeCreated
 * \see \fullref{fct:UnregisterOnNodeCreatedSingleExt
 */
EXPORTDLL int RegisterOnNodeCreatedExt(HController HC, NodeCreatedCallbackFunctionExt callback, void *userpointer);

/**
 * Unregisters a callback for NodeCreate-events.
 *
 * \param HC is the handle to the controller
 * \param callback is the callback function
 *
 * \return
 *     E_OK on success
 *     E_NOEXIST if the callback has not been registered
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnNodeCreatedExt
 * \see \fullref{fct:UnregisterOnNodeCreated
 */
EXPORTDLL int UnregisterOnNodeCreatedSingleExt(HController HC, NodeCreatedCallbackFunctionExt callback);

/**
 * Registers a callback for NodeCreate-events. This callback gets called each
 * time a new node has been created on the controller.
 * \tip{It is possible to have more than 1~callback at a time.}
 *
 * \param HC is the handle to the controller
 * \param callback is the callback function
 *
 * \return
 *     E_OK on success
 *     E_EXIST if the callback has already been registered
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:NodeCreatedCallbackFunction
 * \see \fullref{fct:UnregisterOnNodeCreated
 * \see \fullref{fct:UnregisterOnNodeCreatedSingle
 */
EXPORTDLL int RegisterOnNodeCreated(HController HC, NodeCreatedCallbackFunction callback);

/**
 * Unregisters all callback for NodeCreate-events.
 *
 * \param HC is the handle to the controller
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnNodeCreated
 * \see \fullref{fct:UnregisterOnNodeCreatedSingle
 */
EXPORTDLL int UnregisterOnNodeCreated(HController HC );

/**
 * Unregisters a callback for NodeCreate-events.
 *
 * \param HC is the handle to the controller
 * \param callback is the callback function
 *
 * \return
 *     E_OK on success
 *     E_NOEXIST if the callback has not been registered
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnNodeCreated
 * \see \fullref{fct:UnregisterOnNodeCreated
 */
EXPORTDLL int UnregisterOnNodeCreatedSingle(HController HC, NodeCreatedCallbackFunction callback);

/**
 * Registers a callback for StartOfNodeCreate-events. This callback gets called each
 * time a new set of NodeCreated requests is coming from the controller.
 * \tip{It is possible to have more than 1~callback at a time.}
 *
 * \param HC is the handle to the controller
 * \param callback is the callback function
 * \param userpointer This pointer is passed as a parameter to the callback function
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:StartOfNodeCreatedRequestCallbackFunction
 * \see \fullref{fct:RegisterOnEndOfNodeCreatedRequest
 */
EXPORTDLL int RegisterOnStartOfNodeCreatedRequest(HController HC, StartOfNodeCreatedRequestCallbackFunction callback, void *userpointer);

/**
 * Unregisters a callback for StartOfNodeCreate-events.
 *
 * \param HC is the handle to the controller
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:StartOfNodeCreatedRequestCallbackFunction
 * \see \fullref{fct:RegisterOnStartOfNodeCreatedRequest
 */
EXPORTDLL int UnregisterOnStartOfNodeCreatedRequest(HController HC);

/**
 * Registers a callback for EndOfNodeCreate-events. This callback gets called each
 * time a new set of NodeCreated requests is coming from the controller.
 * \tip{It is possible to have more than 1~callback at a time.}
 *
 * \param HC is the handle to the controller
 * \param callback is the callback function
 * \param userpointer This pointer is passed as a parameter to the callback function
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:EndOfNodeCreatedRequestCallbackFunction
 * \see \fullref{fct:RegisterOnEndOfNodeCreatedRequest
 */
EXPORTDLL int RegisterOnEndOfNodeCreatedRequest(HController HC, EndOfNodeCreatedRequestCallbackFunction callback, void *userpointer);

/**
 * Unregisters a callback for EndOfNodeCreate-events.
 *
 * \param HC is the handle to the controller
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:EndOfNodeCreatedRequestCallbackFunction
 * \see \fullref{fct:RegisterOnStartOfNodeCreatedRequest
 */
EXPORTDLL int UnregisterOnEndOfNodeCreatedRequest(HController HC);

/**
 * Registers a callback for NodeDelete-events. The callback gets called when
 * the given NodeObject was deleted on the controller.
 * \tip{\begin{itemize}\item In order to work correctly, also a ValueChangedCallback has to be registered
 *   for the same variable.
 * \item It is possible to have more than 1~callback at a time.\end{itemize}}
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param callback is the callback function
 * \param userpointer This pointer is passed as a parameter to the callback function
 *
 * \return
 *     E_OK on success
 *     E_EXIST if the callback has already been registered
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:NodeDeletedCallbackFunctionExt
 * \see \fullref{fct:RegisterOnNodeDeleted
 * \see \fullref{fct:RegisterOnNodeDeletedGlobal
 * \see \fullref{fct:UnregisterOnNodeDeleted
 * \see \fullref{fct:UnregisterOnNodeDeletedSingleExt
 * \see \fullref{fct:RegisterOnValueChanged
 */
EXPORTDLL int RegisterOnNodeDeletedExt(HController HC, HNodeObject HNO, NodeDeletedCallbackFunctionExt callback, void *userpointer);

/**
 * Registers a callback for all NodeDelete-events. The callback gets called when
 * any NodeObject was deleted on the controller. 
 * \tip{\begin{itemize}\item This function works only correct, if the Variablecache was enabled
 * with \nameref{fct:EnableVariableCache}.
 * \item Only 1~callback of this type can be registered.\end{itemize}}
 *
 * \param HC is the handle to the controller
 * \param callback is the callback function
 * \param userpointer This pointer is passed as a parameter to the callback function
 *
 * \return
 *     E_OK on success
 *     E_EXIST if the callback has already been registered
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:NodeDeletedCallbackFunctionExt
 * \see \fullref{fct:UnregisterOnNodeDeletedGlobal
 * \see \fullref{fct:RegisterOnNodeDeleted
 * \see \fullref{fct:UnregisterOnNodeDeleted
 * \see \fullref{fct:UnregisterOnNodeDeletedSingleExt
 * \see \fullref{fct:RegisterOnValueChanged
 */
EXPORTDLL int RegisterOnNodeDeletedGlobal(HController HC, NodeDeletedCallbackFunctionExt callback, void *userpointer);

/**
 * Unregisters a callback for the global NodeDelete-events.
 *
 * \param HC is the handle to the controller
 *
 * \return
 *     E_OK on success
 *     E_NOEXIST if a callback has not been registered
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnNodeDeletedExt
 * \see \fullref{fct:UnregisterOnNodeDeleted
 * \see \fullref{fct:RegisterOnNodeDeletedGlobal
 */
EXPORTDLL int UnregisterOnNodeDeletedGlobal(HController HC);

/**
 * Unregisters a callback for the global NodeDelete-events.
 *
 * \param HC is the handle to the controller
 * \param callback is the callback function that shall be unregistered
 *
 * \return
 *     E_OK on success
 *     E_NOEXIST if the callback has not been registered
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnNodeDeletedExt
 * \see \fullref{fct:UnregisterOnNodeDeleted
 * \see \fullref{fct:RegisterOnNodeDeletedGlobal
 */
EXPORTDLL int UnregisterOnNodeDeletedGlobalSingle(HController HC,NodeDeletedCallbackFunctionExt callback);

/**
 * Unregisters a callback for NodeDelete-events.
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param callback is the the callback function
 *
 * \return
 *     E_OK on success
 *     E_NOEXIST if the callback has not been registered
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnNodeDeletedExt
 * \see \fullref{fct:UnregisterOnNodeDeleted
 */
EXPORTDLL int UnregisterOnNodeDeletedSingleExt(HController HC, HNodeObject HNO, NodeDeletedCallbackFunctionExt callback);

/**
 * Registers a callback for NodeDelete-events. The callback gets called when
 * the given NodeObject was deleted on the controller. 
 * \tip{\begin{itemize}\item In order to work correctly, also a ValueChangedCallback has to be registered
 *   for the same variable.
 * \item It is possible to have more than 1~callback at a time.\end{itemize}}
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param callback is the callback function
 *
 * \return
 *     E_OK on success
 *     E_EXIST if a callback of this type has already been registered
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:NodeDeletedCallbackFunction
 * \see \fullref{fct:UnregisterOnNodeDeleted
 * \see \fullref{fct:UnregisterOnNodeDeletedSingle
 * \see \fullref{fct:RegisterOnValueChanged
 */
EXPORTDLL int RegisterOnNodeDeleted(HController HC, HNodeObject HNO, NodeDeletedCallbackFunction callback);

/**
 * Unregisters all callbacks for NodeDelete-events.
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnNodeDeleted
 * \see \fullref{fct:UnregisterOnNodeDeletedSingle
 */
EXPORTDLL int UnregisterOnNodeDeleted(HController HC, HNodeObject HNO);

/**
 * Unregisters a callback for NodeDelete-events.
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param callback is the callback function
 *
 * \return
 *     E_OK on success
 *     E_NOEXIST if a callback of this type has not been registered
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnNodeDeleted
 * \see \fullref{fct:UnregisterOnNodeDeleted
 */
EXPORTDLL int UnregisterOnNodeDeletedSingle(HController HC, HNodeObject HNO, NodeDeletedCallbackFunction callback );

/**
 * Registers a callback for StartOfNodeDeleted-events. The callback gets called each
 * time a new set of NodeDeleted requests is coming from the controller.
 * \tip{It is possible to have more than 1~callback at a time.}
 *
 * \param HC is the handle to the controller
 * \param callback is the callback function
 * \param userpointer This pointer is passed as a parameter to the callback function
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:StartOfNodeDeletedRequestCallbackFunction
 * \see \fullref{fct:RegisterOnEndOfNodeDeletedRequest
 */
EXPORTDLL int RegisterOnStartOfNodeDeletedRequest(HController HC, StartOfNodeDeletedRequestCallbackFunction callback, void *userpointer);

/**
 * Unregisters a callback for StartOfNodeDeleted-events.
 *
 * \param HC is the handle to the controller
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:StartOfNodeDeletedRequestCallbackFunction
 * \see \fullref{fct:RegisterOnStartOfNodeDeletedRequest
 */
EXPORTDLL int UnregisterOnStartOfNodeDeletedRequest(HController HC);

/**
 * Registers a callback for EndOfNodeDeleted-events. The callback gets called each
 * time a new set of NodeDeleted-requests is coming from the controller.
 * \tip{It is possible to have more than 1~callback at a time.}
 *
 * \param HC is the handle to the controller
 * \param callback is the callback function
 * \param userpointer This pointer is passed as a parameter to the callback function
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:EndOfNodeDeletedRequestCallbackFunction
 * \see \fullref{fct:RegisterOnEndOfNodeDeletedRequest
 */
EXPORTDLL int RegisterOnEndOfNodeDeletedRequest(HController HC, EndOfNodeDeletedRequestCallbackFunction callback, void *userpointer);

/**
 * Unregisters a callback for EndOfNodeDeleted-events.
 *
 * \param HC is the handle to the controller
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:EndOfNodeDeletedRequestCallbackFunction
 * \see \fullref{fct:RegisterOnStartOfNodeDeletedRequest
 */
EXPORTDLL int UnregisterOnEndOfNodeDeletedRequest(HController HC);

/**
 * Registers a callback for NameChange-events. The callback gets called when a
 * NodeObject changes its name.
 * \tip{\begin{itemize}\item It is possible to have more than 1~callback for each variable at a time.
 * \item In order to work correctly, also a ValueChangedCallback has to be registered
 *   for the same variable.\end{itemize}}
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param callback is the callback function
 * \param userpointer This pointer is passed as a parameter to the callback function
 *
 * \return
 *     E_OK on success
 *     E_EXIST if the callback has already been registered
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:NameChangeCallbackFunctionExt
 * \see \fullref{fct:RegisterOnNameChanged
 * \see \fullref{fct:UnregisterOnNameChanged
 * \see \fullref{fct:UnregisterOnNameChangedSingleExt
 * \see \fullref{fct:RegisterOnValueChanged
 */
EXPORTDLL int RegisterOnNameChangedExt(HController HC, HNodeObject HNO, NameChangeCallbackFunctionExt callback, void *userpointer);

/**
 * Unregisters a callback for NameChange-events for the given NodeObject.
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param callback is the callback function
 *
 * \return
 *     E_OK on success
 *     E_NOEXIST if the callback has not been registered
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:UnregisterOnNameChanged
 * \see \fullref{fct:RegisterOnNameChangedExt
 */
EXPORTDLL int UnregisterOnNameChangedSingleExt(HController HC, HNodeObject HNO, NameChangeCallbackFunctionExt callback);

/**
 * Registers a callback for NameChange-events.
 *
 * \param HC is the handle to the controller
 * \param callback is the callback function
 * \param userpointer This pointer is given as parameter in the callback function
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:UnregisterOnNameChangedGlobal
 * \see \fullref{fct:UnregisterOnNameChangedGlobalSingle
 */
EXPORTDLL int RegisterOnNameChangedGlobal(HController HC, NameChangeCallbackFunctionExt callback, void *userpointer);

/**
 * Unregisters a callback for NameChange-events.
 *
 * \param HC is the handle to the controller
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnNameChangedGlobal
 * \see \fullref{fct:UnregisterOnNameChangedGlobalSingle
 */
EXPORTDLL int UnregisterOnNameChangedGlobal(HController HC);

/**
 * \todo{Bescheibung}
 *
 * \param HC is the handle to the controller
 * \param callback is the callback function
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnNameChangedGlobal
 * \see \fullref{fct:UnregisterOnNameChangedGlobal
 */
EXPORTDLL int UnregisterOnNameChangedGlobalSingle(HController HC, NameChangeCallbackFunctionExt callback);

/**
 * Registers a callback for NameChange-events. The callback gets called when a
 * NodeObject changes its name.
 * \tip{\begin{itemize}\item It is possible to have more than 1~callback for each variable at a time.
 * \item In order to work correctly, also a ValueChangedCallback has to be registered
 *   for the same variable.\end{itemize}}
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param callback is the callback function
 *
 * \return
 *     E_OK on success
 *     E_EXIST if the callback has already been registered
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:NameChangeCallbackFunction
 * \see \fullref{fct:UnregisterOnNameChanged
 * \see \fullref{fct:UnregisterOnNameChangedSingle
 * \see \fullref{fct:RegisterOnValueChanged
 */
EXPORTDLL int RegisterOnNameChanged(HController HC, HNodeObject HNO, NameChangeCallbackFunction callback);

/**
 * Unregisters a callback for NameChange-events for the given NodeObject.
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param callback is the callback function
 *
 * \return
 *     E_OK on success
 *     E_NOEXIST if the callback has not been registered
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:UnregisterOnNameChanged
 * \see \fullref{fct:RegisterOnNameChanged
 */
EXPORTDLL int UnregisterOnNameChangedSingle(HController HC, HNodeObject HNO, NameChangeCallbackFunction callback);

/**
 * Unregisters all callbacks for NameChange-events for the given NodeObject.
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 *
 * \return
 *     E_OK on success
 *     E_NOEXIST if a callback has not been registered
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:UnregisterOnNameChangedSingle
 * \see \fullref{fct:RegisterOnNameChanged
 */
EXPORTDLL int UnregisterOnNameChanged(HController HC, HNodeObject HNO);

/**
 * Registers a callback for NodeStateChange-events. The callback gets called \newline 
 * when a NodeObject changes its state.
 * \tip{It is possible to have more than 1~callback for each variable.}
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param callback is the callback function
 * \param userpointer This pointer is passed as a parameter to the callback function
 *
 * \return
 *     E_OK on success
 *     E_EXIST if the callback has already been registered
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:NodeStateChangeCallbackFunction
 * \see \fullref{fct:UnregisterOnNodeStateChanged
 * \see \fullref{fct:UnregisterOnNodeStateChangedSingle
 */
EXPORTDLL int RegisterOnNodeStateChanged(HController HC, HNodeObject HNO, NodeStateChangeCallbackFunction callback, void *userpointer);

/**
 * \todo{Beschreibung}
 *
 * \param HC is the handle to the controller
 * \param callback is the callback function
 * \param userpointer This pointer is given as parameter in the callback function
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:UnregisterOnNodeStateChangedGlobal
 */
EXPORTDLL int RegisterOnNodeStateChangedGlobal(HController HC, NodeStateChangeCallbackFunction callback, void *userpointer);

/**
 * \todo{Beschreibung}
 *
 * \param HC is the handle to the controller
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnNodeStateChangedGlobal
 */
EXPORTDLL int UnregisterOnNodeStateChangedGlobal(HController HC);

/**
 * Unregisters a callback for NodeStateChange-events for the given NodeObject.
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 * \param callback is the callback function
 *
 * \return
 *     E_OK on success
 *     E_NOEXIST if the callback has not been registered
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:UnregisterOnNodeStateChanged
 * \see \fullref{fct:RegisterOnNodeStateChanged
 */
EXPORTDLL int UnregisterOnNodeStateChangedSingle(HController HC, HNodeObject HNO, NodeStateChangeCallbackFunction callback);

/**
 * Unregisters all callbacks for NodeStateChanged-events for the given NodeObject.
 *
 * \param HC is the handle to the controller
 * \param HNO is the handle to the NodeObject
 *
 * \return
 *     E_OK on success
 *     E_NOEXIST if a callback has not been registered
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:UnregisterOnNodeStateChangedSingle
 * \see \fullref{fct:RegisterOnNodeStateChanged
 */
EXPORTDLL int UnregisterOnNodeStateChanged(HController HC, HNodeObject HNO);

/**
 * Registers a callback for a PLCChange-event. Each time the PLC state changes
 * on the controller the callback function gets called. After registration the callback is being
 * called initially with the current PLC state.
 * \tip{It is possible to have more than 1~callback at a time.}
 *
 * \param HC is the handle to the controller
 * \param callback is the callback function
 * \param userpointer This pointer is passed as a parameter to the callback function
 *
 * \return
 *     E_OK on success
 *     E_EXIST if the callback has already been registered
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:PLCChangeCallbackFunctionExt
 * \see \fullref{fct:RegisterOnPLCChanged
 * \see \fullref{fct:UnregisterOnPLCChanged
 * \see \fullref{fct:UnregisterOnPLCChangedSingle
 */
EXPORTDLL int RegisterOnPLCChangedExt(HController HC, PLCChangeCallbackFunctionExt callback, void *userpointer);

/**
 * Unregisters a callback for a PLCChange-event.
 *
 * \param HC is the handle to the controller
 * \param callback is the callback function
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnPLCChangedExt
 * \see \fullref{fct:UnregisterOnPLCChanged
 */
EXPORTDLL int UnregisterOnPLCChangedSingleExt(HController HC, PLCChangeCallbackFunctionExt callback);

/**
 * Registers a callback for a PLCChange-event. Each time the PLC state changes
 * on the controller the callback function gets called. After registation the callback is being
 * called initially with the current PLC state.
 * \tip{It is possible to have more than 1~callback at a time.}
 *
 * \param HC is the handle to the controller
 * \param callback is the callback function
 *
 * \return
 *     E_OK on success
 *     E_EXIST if the callback has already been registered
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:PLCChangeCallbackFunction
 * \see \fullref{fct:UnregisterOnPLCChanged
 * \see \fullref{fct:UnregisterOnPLCChangedSingle
 */
EXPORTDLL int RegisterOnPLCChanged(HController HC, PLCChangeCallbackFunction callback);

/**
 * Unregisters all callbacks for a PLC-event.
 *
 * \param HC is the handle to the controller
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnPLCChanged
 * \see \fullref{fct:UnregisterOnPLCChangedSingle
 */
EXPORTDLL int UnregisterOnPLCChanged(HController HC);

/**
 * Unregisters a callback for a PLC-event.
 *
 * \param HC is the handle to the controller
 * \param callback is the callback function
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnPLCChanged
 * \see \fullref{fct:UnregisterOnPLCChanged
 */
EXPORTDLL int UnregisterOnPLCChangedSingle(HController HC, PLCChangeCallbackFunction callback);

/**
 * \notice{This function is deprecated.}{\vspace{-1.5ex}}{\item[\textbullet] Use \nameref{fct:RegisterOnSystemMessage} instead.}
 * Registers a callback for a SystemMessages-event. Each time a SystemMessage occurs the
 * callback gets called. 
 * \tip{It is possible to have more than 1~callback at a time.}
 *
 * \param HC is the handle to the controller
 * \param callback is the callback function
 * \param userpointer This pointer is passed as a parameter to the callback function
 *
 * \return
 *     E_OK on success
 *     E_EXIST if the callback has already been registered
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:SysMsgCallbackFunctionExt
 * \see \fullref{fct:UnregisterOnSysMsgSingleExt
 * \see \fullref{fct:UnregisterOnSysMsg
 * \see \fullref{fct:GetSysMsgRawText
 */
EXPORTDLL int RegisterOnSysMsgExt(HController HC, SysMsgCallbackFunctionExt callback, void *userpointer);

/**
 * \notice{This function is deprecated.}{\vspace{-1.5ex}}{\item[\textbullet] Use \nameref{fct:UnregisterOnSystemMessageSingle} instead.}
 * Unregisters a callback for a SystemMessage-event.
 *
 * \param HC is the handle to the controller
 * \param callback is the callback function
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnSysMsgExt
 * \see \fullref{fct:UnregisterOnSysMsg
 */
EXPORTDLL int UnregisterOnSysMsgSingleExt(HController HC, SysMsgCallbackFunctionExt callback);

/**
 * \notice{This function is deprecated.}{\vspace{-1.5ex}}{\item[\textbullet] Use \nameref{fct:RegisterOnSystemMessage} instead.}
 * Registers a callback for a SystemMessages-event. Each time a SystemMessage occurs the
 * callback gets called. 
 * \tip{It is possible to have more than 1~callback at a time.}
 *
 * \param HC is the handle to the controller
 * \param callback is the callback function
 *
 * \return
 *     E_OK on success
 *     E_EXIST if the callback has already been registered
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:SysMsgCallbackFunction
 * \see \fullref{fct:RegisterOnSysMsgExt
 * \see \fullref{fct:UnregisterOnSysMsg
 * \see \fullref{fct:GetSysMsgRawText
 */
EXPORTDLL int RegisterOnSysMsg(HController HC,SysMsgCallbackFunction callback);

/**
 * \notice{This function is deprecated.}{\vspace{-1.5ex}}{\item[\textbullet] Use \nameref{fct:UnregisterOnSystemMessage} instead.}
 * Unregisters all callbacks for a SystemMessage-event.
 *
 * \param HC is the handle to the controller
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnSystemMessage
 */
EXPORTDLL int UnregisterOnSysMsg(HController HC);

/**
 * Unregisters a callback for a SystemMessage-event.
 * \notice{This function is deprecated.}{\vspace{-1.5ex}}{\item[\textbullet] Use \nameref{fct:UnregisterOnSystemMessageSingle} instead.}
 *
 * \param HC is the handle to the controller
 * \param callback is the callback function
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnSysMsg
 */
EXPORTDLL int UnregisterOnSysMsgSingle(HController HC, SysMsgCallbackFunction callback);

/**
 * Registers a callback for a SystemMessages-event. Each time a SystemMessage occurs the
 * callback gets called. 
 * \tip{It is possible to have more than 1~callback at a time.}
 *
 * \param callback is the callback function
 * \param userpointer This pointer is passed as a parameter to the callback function
 *
 * \return
 *     E_OK on success
 *     E_EXIST if the callback has already been registered
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:SystemMessageCallbackFunction
 * \see \fullref{fct:UnregisterOnSystemMessage
 * \see \fullref{fct:UnregisterOnSystemMessageSingle
 * \see \fullref{fct:GetSystemMessageXML
 */
EXPORTDLL int RegisterOnSystemMessage(SystemMessageCallbackFunction callback, void *userpointer);

/**
 * Unregisters all callbacks for a SystemMessage-event.
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnSystemMessage
 */
EXPORTDLL int UnregisterOnSystemMessage(void);

/**
 * Unregisters a callback for a SystemMessage-event.
 *
 * \param callback is the callback function
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnSystemMessage
 * \see \fullref{fct:UnregisterOnSystemMessage
 */
EXPORTDLL int UnregisterOnSystemMessageSingle(SystemMessageCallbackFunction callback);

/**
 * Registers a callback for a DeviceCreated-event. The callback gets called
 * when a device was created on the controller.
 * \tip{Due to design it is not ensured that the device is completely
 * created when the callback gets called. This behavior depends on the
 * version of the firmware.}
 *
 * \param HC is the handle to the controller
 * \param callback is the callback function
 * \param userpointer This pointer is passed as a parameter to the callback function
 *
 * \return
 *     E_OK on success
 *     E_EXIST if the callback has already been registered
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:UnregisterOnDeviceCreated
 * \see \fullref{fct:RegisterOnDeviceDeleted
 */
EXPORTDLL int RegisterOnDeviceCreated(HController HC, DeviceCreatedCallbackFunction callback, void *userpointer);

/**
 * Unregisters a callback for a DeviceCreated-event.
 *
 * \param HC is the handle to the controller
 * \param callback is the callback function
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnDeviceCreated
 * \see \fullref{fct:RegisterOnDeviceDeleted
 */
EXPORTDLL int UnregisterOnDeviceCreated(HController HC, DeviceCreatedCallbackFunction callback);

/**
 * Registers a callback for a DeviceDeleted-event. The callback gets called
 * when a device was created on the controller.
 *
 * \param HC is the handle to the controller
 * \param callback is the callback function
 * \param userpointer This pointer is passed as a parameter to the callback function
 *
 * \return
 *     E_OK on success
 *     E_EXIST if the callback has already been registered
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:UnregisterOnDeviceDeleted
 * \see \fullref{fct:RegisterOnDeviceCreated
 */
EXPORTDLL int RegisterOnDeviceDeleted(HController HC, DeviceDeletedCallbackFunction callback, void *userpointer);

/**
 * Unregisters a callback for a DeviceDeleted-event. The callback gets called
 * when a device was created on the controller.
 *
 * \param HC is the handle to the controller
 * \param callback is the callback function
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnDeviceDeleted
 * \see \fullref{fct:RegisterOnDeviceCreated
 */
EXPORTDLL int UnregisterOnDeviceDeleted(HController HC, DeviceDeletedCallbackFunction callback);

/**
 * \todo{Beschreibung}
 *
 * \param HC is the handle to the controller
 * \param callback is the callback function
 * \param userpointer This pointer is passed as a parameter to the callback function
 *
 * \return
 *     E_OK on success
 *     E_EXIST if the callback has already been registered
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:UnregisterOnDeviceActivated
 * \see \fullref{fct:RegisterOnDeviceDeactivated
 */
EXPORTDLL int RegisterOnDeviceActivated(HController HC, DeviceActivatedCallbackFunction callback, void *userpointer);

/**
 * \todo{Beschreibung}
 *
 * \param HC is the handle to the controller
 * \param callback is the callback function
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnDeviceActivated
 * \see \fullref{fct:RegisterOnDeviceDeactivated
 */
EXPORTDLL int UnregisterOnDeviceActivated(HController HC, DeviceActivatedCallbackFunction callback);

/**
 * \todo{Beschreibung}
 *
 * \param HC is the handle to the controller
 * \param callback is the callback function
 * \param userpointer This pointer is passed as a parameter to the callback function
 *
 * \return
 *     E_OK on success
 *     E_EXIST if the callback has already been registered
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:UnregisterOnDeviceDeactivated
 * \see \fullref{fct:RegisterOnDeviceActivated
 */
EXPORTDLL int RegisterOnDeviceDeactivated(HController HC, DeviceDeactivatedCallbackFunction callback, void *userpointer);

/**
 * \todo{Beschreibung}
 *
 * \param HC is the handle to the controller
 * \param callback is the callback function
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnDeviceActivated
 * \see \fullref{fct:RegisterOnDeviceDeactivated
 */
EXPORTDLL int UnregisterOnDeviceDeactivated(HController HC, DeviceDeactivatedCallbackFunction callback);

/**
 * \todo{Beschreibung}
 *
 * \param HC is the handle to the controller
 * \param callback is the callback function
 * \param userpointer This pointer is passed as a parameter to the callback function
 *
 * \return
 *     E_OK on success
 *     E_EXIST if the callback has already been registered
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:UnregisterOnDeviceDependencyAdded
 * \see \fullref{fct:RegisterOnDeviceDependencyRemoved
 */
EXPORTDLL int RegisterOnDeviceDependencyAdded(HController HC, DeviceDependencyAddedCallbackFunction callback, void *userpointer);

/**
 * \todo{Beschreibung}
 *
 * \param HC is the handle to the controller
 * \param callback is the callback function
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnDeviceDependencyRemoved
 * \see \fullref{fct:RegisterOnDeviceDependencyAdded
 */
EXPORTDLL int UnregisterOnDeviceDependencyAdded(HController HC, DeviceDependencyAddedCallbackFunction callback);

/**
 * \todo{Beschreibung}
 *
 * \param HC is the handle to the controller
 * \param callback is the callback function
 * \param userpointer This pointer is passed as a parameter to the callback function
 *
 * \return
 *     E_OK on success
 *     E_EXIST if the callback was already registered
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:UnregisterOnDeviceDependencyRemoved
 * \see \fullref{fct:RegisterOnDeviceDependencyAdded
 */
EXPORTDLL int RegisterOnDeviceDependencyRemoved(HController HC, DeviceDependencyRemovedCallbackFunction callback, void *userpointer);

/**
 * \todo{Beschreibung}
 *
 * \param HC is the handle to the controller
 * \param callback is the callback function
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnDeviceDependencyAdded
 * \see \fullref{fct:RegisterOnDeviceDependencyRemoved
 */
EXPORTDLL int UnregisterOnDeviceDependencyRemoved(HController HC, DeviceDependencyRemovedCallbackFunction callback);

/**
 * \todo{Beschreibung}
 *
 * \param HC is the handle to the controller
 * \param callback is the callback function
 * \param userpointer This pointer is passed as a parameter to the callback function
 *
 * \return
 *     E_OK on success
 *     E_EXIST if the callback has already been registered
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:UnregisterOnDeviceStateChanged
 */
EXPORTDLL int RegisterOnDeviceStateChanged(HController HC, DeviceStateChangedCallbackFunction callback, void *userpointer);

/**
 * \todo{Beschreibung}
 *
 * \param HC is the handle to the controller
 * \param callback is the callback function
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnDeviceStateChanged
 */
EXPORTDLL int UnregisterOnDeviceStateChanged(HController HC, DeviceStateChangedCallbackFunction callback);

/**
 * \todo{Beschreibung}
 *
 * \param HC is the handle to the controller
 * \param callback is the callback function
 * \param userpointer This pointer is passed as a parameter to the callback function
 *
 * \return
 *     E_OK on success
 *     E_EXIST if the callback has already been registered
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:UnregisterOnDeviceErrorStateChanged
 */
EXPORTDLL int RegisterOnDeviceErrorStateChanged(HController HC, DeviceErrorStateChangedCallbackFunction callback, void *userpointer);

/**
 * \todo{Beschreibung}
 *
 * \param HC is the handle to the controller
 * \param callback is the callback function
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnDeviceErrorStateChanged
 */
EXPORTDLL int UnregisterOnDeviceErrorStateChanged(HController HC, DeviceErrorStateChangedCallbackFunction callback);

/**
 * \todo{Beschreibung}
 *
 * \param HC is the handle to the controller
 * \param callback is the callback function
 * \param userpointer This pointer is passed as a parameter to the callback function
 *
 * \return
 *     E_OK on success
 *     E_EXIST if the callback has already been registered
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:UnregisterOnDeviceParamStateChanged
 */
EXPORTDLL int RegisterOnDeviceParamStateChanged(HController HC, DeviceParamStateChangedCallbackFunction callback, void *userpointer);

/**
 * \todo{Beschreibung}
 *
 * \param HC is the handle to the controller
 * \param callback is the callback function
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnDeviceParamStateChanged
 */
EXPORTDLL int UnregisterOnDeviceParamStateChanged(HController HC, DeviceParamStateChangedCallbackFunction callback);

/**
 * \todo{Beschreibung}
 *
 * \param HC is the handle to the controller
 * \param callback is the callback function
 * \param userpointer This pointer is passed as a parameter to the callback function
 *
 * \return
 *     E_OK on success
 *     E_EXIST if the callback has already been registered
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:UnregisterOnDevicePowerStateChanged
 */
EXPORTDLL int RegisterOnDevicePowerStateChanged(HController HC, DevicePowerStateChangedCallbackFunction callback, void *userpointer);

/**
 * \todo{Beschreibung}
 *
 * \param HC is the handle to the controller
 * \param callback is the callback function
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnDevicePowerStateChanged
 */
EXPORTDLL int UnregisterOnDevicePowerStateChanged(HController HC, DevicePowerStateChangedCallbackFunction callback);

/**
 * \todo{Beschreibung}
 *
 * \param HC is the handle to the controller
 * \param callback is the callback function
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:UnregisterOnJobLinesSingle
 * \see \fullref{fct:UnregisterOnJobLines
 */
EXPORTDLL int RegisterOnJobLines(HController HC, JobLinesCallbackFunction callback);

/**
 * \todo{Beschreibung}
 *
 * \param HC is the handle to the controller
 * \param callback is the callback function
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnJobLines
 * \see \fullref{fct:UnregisterOnJobLines
 */
EXPORTDLL int UnregisterOnJobLinesSingle(HController HC, JobLinesCallbackFunction callback);

/**
 * \todo{Beschreibung}
 *
 * \param HC is the handle to the controller
 * \param callback is the callback function
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnJobLines
 * \see \fullref{fct:UnregisterOnJobLinesSingle
 */
EXPORTDLL int UnregisterOnJobLines(HController HC);

/**
 * \todo{Beschreibung}
 *
 * \param HC is the handle to the controller
 * \param callback is the callback function
 * \param userpointer This pointer will be a parameter in every callback of this type
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:UnregisterOnTssDataSingle
 * \see \fullref{fct:UnregisterOnTssData
 */
EXPORTDLL int RegisterOnTssData(HController HC, TssDataCallbackFunction callback, void *userpointer);

/**
 * \todo{Beschreibung}
 *
 * \param HC is the handle to the controller
 * \param callback is the callback function
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnTssData
 * \see \fullref{fct:UnregisterOnTssData
 */
EXPORTDLL int UnregisterOnTssDataSingle(HController HC, TssDataCallbackFunction callback);

/**
 * \todo{Beschreibung}
 *
 * \param HC is the handle to the controller
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnTssData
 * \see \fullref{fct:UnregisterOnTssDataSingle
 */
EXPORTDLL int UnregisterOnTssData(HController HC);

/**
 * \todo{Beschreibung}
 *
 * \param HC is the handle to the controller
 * \param callback is the callback function
 * \param userpointer This pointer will be a parameter in every callback of this type
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:UnregisterOnUpdateTssChannelsSingle
 * \see \fullref{fct:UnregisterOnUpdateTssChannels
 */
EXPORTDLL int RegisterOnUpdateTssChannels(HController HC, UpdateTssChannelsCallbackFunction callback, void *userpointer);

/**
 * \todo{Beschreibung}
 *
 * \param HC is the handle to the controller
 * \param callback is the callback function
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnUpdateTssChannels
 * \see \fullref{fct:UnregisterOnUpdateTssChannels
 */
EXPORTDLL int UnregisterOnUpdateTssChannelsSingle(HController HC, UpdateTssChannelsCallbackFunction callback);

/**
 * \todo{Beschreibung}
 *
 * \param HC is the handle to the controller
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnUpdateTssChannels
 * \see \fullref{fct:UnregisterOnUpdateTssChannelsSingle
 */
EXPORTDLL int UnregisterOnUpdateTssChannels(HController HC);

/**
 * Gets the length of a given SysMsg as XML.
 * \tip{This call is only valid from inside a \nameref{fct:SysMsgCallbackFunction}.}
 *
 * \param HSM is the handle to the SysMsg
 *
 * \return
 *     the strlen of the SysMsg without the trailing 0-byte
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetSystemMessageXML
 * \see \fullref{fct:SysMsgCallbackFunction
 */
EXPORTDLL int GetSystemMessageXMLLen(HSysMsg HSM);

/**
 * Gets the XML-text of a SysMsg. Call \nameref{fct:GetSysMsgXMLLen} to get the
 * bufferlength. If the function returns \texttt{E_OK} the \texttt{buffer}
 * holds the \texttt{NULL}-termi\-nated string. To work correctly the buffer length has to
 * be \texttt{GetSysMsgXMLLen+1}. \newline 
 * SysMessages are divided into contexts and messages. 
 * Contexts are optional.
 * \begin{lstlisting}[language=xml,caption={SysMessage with neither context nor parameters}]
 * <?xml version="1.0" encoding="UTF-8" ?>
 * <SYSMESSAGE ID="3" ControllerHandle="1" Date="2010-11-17" Time="07:51:27" Uptime="122.232921">
 *   <MESSAGE Name="FW_E_BREAK" Caller="HandleSysMsg" Facility="Firmware"/>
 * </SYSMESSAGE>
 * \end{lstlisting}
 * \begin{lstlisting}[language=xml,caption={SysMessage with 1 context and 1 message with parameters}]
 * <?xml version="1.0" encoding="UTF-8" ?>
 * <SYSMESSAGE ID="1" ControllerHandle="1" Date="2010-11-17" Time="07:51:27" Uptime="122.232453">
 *   <CONTEXT Name="Execute Script usr.job.Job.Script">
 *     <MESSAGE Name="FW_SCRIPT_ERROR" Caller="HandleSysMsg" Facility="Firmware">
 *       <PARAM Name="ERRORDESCRIPTION" Value="syntax error"/>
 *       <PARAM Name="LINE" Value="1"/>
 *       <PARAM Name="COLUMN" Value="9"/>
 *     </MESSAGE>
 *   </CONTEXT>
 * </SYSMESSAGE>
 * \end{lstlisting}
 * \begin{lstlisting}[language=xml,caption={SysMessage with 3 contexts and 1 message}]
 * <?xml version="1.0" encoding="UTF-8" ?>
 * <SYSMESSAGE ID="1" ControllerHandle="1" Date="2010-11-17" Time="07:51:27" Uptime="122.232453">
 *   <CONTEXT Name="Execute Conditional usr.job.Job.Conditional">
 *     <CONTEXT Name="Execute Conditional usr.job.Job.Conditional.Conditional">
 *       <CONTEXT Name="Execute Conditional usr.job.Job.Conditional.Conditional.Script">
 *         <MESSAGE Name="FW_SCRIPT_ERROR" Caller="HandleSysMsg" Facility="Firmware">
 *           <PARAM Name="ERRORDESCRIPTION" Value="syntax error"/>
 *           <PARAM Name="LINE" Value="1"/>
 *           <PARAM Name="COLUMN" Value="9"/>
 *         </MESSAGE>
 *       </CONTEXT>
 *     </CONTEXT>
 *   </CONTEXT>
 * </SYSMESSAGE>
 * \end{lstlisting}
 * The \texttt{SYSMESSAGE} tag can hold \texttt{CONTEXT} or \texttt{MESSAGE} child tags. The attribute
 * \texttt{ID} is a unique identifier for the complete message. The attribute
 * \texttt{Con\-troller\-Han\-dle} gives the handle to the controller if the message 
 * can be dedicated to a controller, otherwise it is omitted. The attribute
 * \texttt{Date} gives the date the SysMessage occured in the form \texttt{yyyy-mm-dd}. The
 * attribute \texttt{Time} gives the time the SysMessage occured in the form
 * \texttt{hh:mm:ss}. The attribute \texttt{Uptime} gives the time the
 * ControllerLib application is running in seconds.
 * 
 * A \texttt{CONTEXT} tag can hold another \texttt{CONTEXT} or a \texttt{MESSAGE} child tag. Its only
 * attribute \texttt{Name} gives the name of the context.
 * 
 * A \texttt{MESSAGE} tag can hold a variable count of \texttt{PARAM} tags. The attribute
 * \texttt{Name} gives the name of the SysMessage. This is the primary key in a
 * database where the complete text of the message can be found. The attribute
 * \texttt{Caller} tells, where the SysMessage was thrown. Normally, this is the
 * function name. The attribute \texttt{Facility} tells, in which facility the
 * SysMessage was thrown. This can be \texttt{Firmware}, \texttt{ControllerLib} or
 * \texttt{ClientApplication}.
 * 
 * A \texttt{PARAM} tag can hold any child tags. It is used to fill out variables in
 * the complete message text found in the database. The attribute \texttt{Name}
 * gives the name of the variable to change and the attribute \texttt{Value} gives
 * the value the variable should be changed to.
 * \begin{lstlisting}[caption={GetSystemMessageXML example}]
 * int buflen = GetSysMsgXMLLen(HC, HSM);
 * if (buflen > 0) {
 *   char *buffer = (char*)malloc(++buflen);
 *   if (GetSysMsgXML(HC, HSM, buffer, buflen) == E_OK) {
 *     printf("MessageXML:\n %s\n", buffer);
 *   }
 *   free(buffer);
 * }
 * \end{lstlisting}
 * \tip{This call is only valid from inside a \nameref{fct:SysMsgCallbackFunction}.}
 *
 * \param HSM is the handle to the SysMsg
 * \param buffer is the pointer to the buffer for the SysMsg
 * \param bufferlen is the size of the buffer in bytes
 *
 * \return
 *     
 *     E_NOSPACE if the buffer was too small
 *     E_UNAVAIL if the message is not available
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetSystemMessageXMLLen
 * \see \fullref{fct:SysMsgCallbackFunction
 */
EXPORTDLL int GetSystemMessageXML(HSysMsg HSM, char *buffer,	int bufferlen);

/**
 * \notice{This function is deprecated.}{\vspace{-1.5ex}}{\item[\textbullet] Use \nameref{fct:GetSystemMessageXMLLen} instead.}
 * Gets the length of a given SysMsg as XML.
 * \tip{This call is only valid from inside a \nameref{fct:SysMsgCallbackFunction}.}
 *
 * \param HC is the handle to the controller
 * \param HSM is the handle to the SysMsg
 *
 * \return
 *     the strlen of the SysMsg without the trailing 0-byte
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetSysMsgRawTextLen
 * \see \fullref{fct:SysMsgCallbackFunction
 */
EXPORTDLL int GetSysMsgXMLLen(HController HC, HSysMsg HSM);

/**
 * \notice{This function is deprecated.}{\vspace{-1.5ex}}{\item[\textbullet] Use \nameref{fct:GetSystemMessageXML} instead.}
 * Gets the XML text of a SysMsg. Call \nameref{fct:GetSysMsgXMLLen} to get the
 * buffer length. If the function returns \texttt{E_OK} the \texttt{buffer}
 * holds the \texttt{NULL}-terminated string. To work correctly the buffer length has to
 * be \texttt{GetSysMsgXMLLen+1}. \newpage
 * SysMessages are divided in contexts and
 * messages. Contexts are optional.
 * \begin{lstlisting}[language=xml,caption={SysMessage with neither context nor parameters}]
 * <?xml version="1.0" encoding="UTF-8" ?>
 * <SYSMESSAGE ID="3" Date="2010-11-17" Time="07:51:27" Uptime="122.232921">
 *   <MESSAGE Name="FW_E_BREAK" Caller="HandleSysMsg" Facility="Firmware"/>
 * </SYSMESSAGE>
 * \end{lstlisting}
 * \begin{lstlisting}[language=xml,caption={SysMessage with 1 context and 1 message with parameters}]
 * <?xml version="1.0" encoding="UTF-8" ?>
 * <SYSMESSAGE ID="1" Date="2010-11-17" Time="07:51:27" Uptime="122.232453">
 *   <CONTEXT Name="Execute Script usr.job.Job.Script">
 *     <MESSAGE Name="FW_SCRIPT_ERROR" Caller="HandleSysMsg" Facility="Firmware">
 *       <PARAM Name="ERRORDESCRIPTION" Value="syntax error"/>
 *       <PARAM Name="LINE" Value="1"/>
 *       <PARAM Name="COLUMN" Value="9"/>
 *     </MESSAGE>
 *   </CONTEXT>
 * </SYSMESSAGE>
 * \end{lstlisting}
 * \begin{lstlisting}[language=xml,caption={SysMessage with 3 contexts and 1 message}]
 * <?xml version="1.0" encoding="UTF-8" ?>
 * <SYSMESSAGE ID="1" Date="2010-11-17" Time="07:51:27" Uptime="122.232453">
 *   <CONTEXT Name="Execute Conditional usr.job.Job.Conditional">
 *     <CONTEXT Name="Execute Conditional usr.job.Job.Conditional.Conditional">
 *       <CONTEXT Name="Execute Conditional usr.job.Job.Conditional.Conditional.Script">
 *         <MESSAGE Name="FW_SCRIPT_ERROR" Caller="HandleSysMsg" Facility="Firmware">
 *           <PARAM Name="ERRORDESCRIPTION" Value="syntax error"/>
 *           <PARAM Name="LINE" Value="1"/>
 *           <PARAM Name="COLUMN" Value="9"/>
 *         </MESSAGE>
 *       </CONTEXT>
 *     </CONTEXT>
 *   </CONTEXT>
 * </SYSMESSAGE>
 * \end{lstlisting}
 * The \texttt{SYSMESSAGE} tag can hold \texttt{CONTEXT} or \texttt{MESSAGE} child tags. The attribute
 * \texttt{ID} is a unique identifier for the complete message. The attribute
 * \texttt{Date} gives the date the SysMessage occured in the form \texttt{yyyy-mm-dd}. The
 * attribute \texttt{Time} gives the time the SysMessage occured in the form
 * \texttt{hh:mm:ss}. The attribute \texttt{Uptime} gives the time the
 * ControllerLib application is running in seconds.
 * 
 * A \texttt{CONTEXT} tag can hold another \texttt{CONTEXT} or a \texttt{MESSAGE} child tag. Its only
 * attribute \texttt{Name} gives the name of the context.
 * 
 * A \texttt{MESSAGE} tag can hold a variable count of \texttt{PARAM}  tags. The attribute
 * \texttt{Name} gives the name of the SysMessage. This is the primary key in a
 * database where the complete text of the message can be found. The attribute
 * \texttt{Caller} tells, where the SysMessage was thrown. Normally, this is the
 * function name. The attribute \texttt{Facility} tells, in which facility the
 * SysMessage was thrown. This can be \texttt{Firmware}, \texttt{ControllerLib} or
 * \texttt{ClientApplication}.
 * 
 * A \texttt{PARAM}  tag can hold any child tags. It is used to fill out variables in
 * the complete message text found in the database. The attribute \texttt{Name}
 * gives the name of the variable to change and the attribute \texttt{Value} gives
 * the value the variable should be changed to.
 * \begin{lstlisting}[caption={GetSysMsgXML example}]
 * int buflen = GetSysMsgXMLLen(HC, HSM);
 * if (buflen > 0) {
 *   char *buffer = (char*)malloc(++buflen);
 *   if (GetSysMsgXML(HC, HSM, buffer, buflen) == E_OK) {
 *     printf("MessageXML:\n %s\n", buffer);
 *   }
 *   free(buffer);
 * }
 * \end{lstlisting}
 * \tip{This call is only valid from inside a \nameref{fct:SysMsgCallbackFunction}.}
 *
 * \param HC is the handle to the controller
 * \param HSM is handle to the SysMsg
 * \param buffer is the pointer to the buffer for the SysMsg
 * \param bufferlen is the size of the buffer in bytes
 *
 * \return
 *     
 *     E_NOSPACE if the buffer was too small
 *     E_UNAVAIL if the message is not available
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetSysMsgXMLLen
 * \see \fullref{fct:GetSysMsgRawText
 * \see \fullref{fct:SysMsgCallbackFunction
 */
EXPORTDLL int GetSysMsgXML(HController HC, HSysMsg HSM, char *buffer,	int bufferlen);

/**
 * \notice{This function is deprecated.}{\vspace{-1.5ex}}{\item[\textbullet] Use \nameref{fct:GetSystemMessageXMLLen} instead.}
 * Gets the length of a given SysMsg.
 * \tip{This call is only valid from inside a \nameref{fct:SysMsgCallbackFunction}.}
 *
 * \param HC is the handle to the controller
 * \param HSM is the handle to the SysMsg
 *
 * \return
 *     the strlen of the SysMsg without the trailing 0-byte
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetSysMsgRawText
 * \see \fullref{fct:GetSysMsgXMLLen
 * \see \fullref{fct:GetSysMsgXML
 * \see \fullref{fct:SysMsgCallbackFunction
 */
EXPORTDLL int GetSysMsgRawTextLen(HController HC, HSysMsg HSM);

/**
 * Gets the raw text of a SysMsg. Call \nameref{fct:GetSysMsgRawTextLen} to get the
 * buffer length. If the function returns \texttt{E_OK} the \texttt{buffer}
 * holds the \texttt{NULL}-terminated string. To work correctly the buffer length has to
 * be \texttt{GetSysMsgRawTextLen+1}.  Messages are divided in contexts and
 * messages. Each context and each message have a \texttt{NAME} and \texttt{LEVEL}. The \texttt{NAME}
 * identifies the context or message and the \texttt{LEVEL} gives the hierarchical
 * information. Message blocks also hold information about the number of
 * parameters and the parameters itself. Context blocks are optional. Each
 * block can occur multiple times.
 * 
 * \begin{lstlisting}[caption={Context blocks are of this form}]
 * CONTEXTBEGIN
 *   NAME=<string>
 *   LEVEL=<number>
 * CONTEXTEND
 * \end{lstlisting}
 * 
 * \begin{lstlisting}[caption={Message blocks are of this form}]
 * MESSAGEBEGIN
 *   NAME=<string>
 *   LEVEL=<number>
 *   ARGC=<number>
 *   <string>=<variant>
 * MESSAGEEND
 * \end{lstlisting}
 * 
 * \begin{lstlisting}[caption={Complete message string example}]
 * CONTEXTBEGIN
 *   NAME=Execute Script usr.job.Job.Script
 *   LEVEL=0
 * CONTEXTEND
 * MESSAGEBEGIN
 *   NAME=FW_SCRIPT_ERROR
 *   LEVEL=1
 *   ARGC=3
 *   ERRORDESCRIPTION=missing 'C'
 *   LINE=1
 *   COLUMN=3
 * MESSAGEEND
 * MESSAGEBEGIN
 *   NAME=FW_SCRIPT_ERROR
 *   LEVEL=1
 *   ARGC=3
 *   ERRORDESCRIPTION=syntax error
 *   LINE=1
 *   COLUMN=3
 * MESSAGEEND
 * \end{lstlisting}
 * 
 * \begin{lstlisting}[caption={GetSysMsgRawText call example}]
 * int buflen = GetSysMsgRawTextLen(HC, HSM);
 * if (buflen > 0) {
 *   char *buffer = (char*)malloc(++buflen);
 *   if (GetSysMsgRawText(HC, HSM, buffer, buflen) == E_OK) {
 *     printf("Messagerawtext: %s\n", buffer);
 *   }
 *   free(buffer);
 * }
 * \end{lstlisting}
 * \tip{This call is only valid from inside a \nameref{fct:SysMsgCallbackFunction}.}
 *
 * \param HC is the handle to the controller
 * \param HSM is the handle to the SysMsg
 * \param buffer is the pointer to the buffer for the SysMsg
 * \param bufferlen is the size of the buffer in bytes
 *
 * \return
 *     
 *     E_NOSPACE if the buffer was too small
 *     E_UNAVAIL if the message is not available
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetSysMsgRawTextLen
 * \see \fullref{fct:GetSysMsgXML
 * \see \fullref{fct:SysMsgCallbackFunction
 */
EXPORTDLL int GetSysMsgRawText(HController HC, HSysMsg HSM, char *buffer,	int bufferlen);

/**
 * Gets the number of (scan) heads managed by the controller.
 *
 * \param HC is the handle to the controller
 *
 * \return
 *     the number of (scan) heads
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetHead
 */
EXPORTDLL int GetHeadCount(HController HC);

/**
 * Gets a specific (scan) head. Information about (scan) heads is needed for distortion or scan field correction.
 *
 * \param HC is the handle to the controller
 * \param index is the index of the (scan) head. The index starts at texttt0
 *
 * \return
 *     the handle to the (scan) head
 *     ARG_INVALID_HANDLE_VALUE on failure
 *
 * \see \fullref{fct:GetHeadCount
 */
EXPORTDLL HHead GetHead(HController HC, int index);

/**
 * Loads a distortion file to the controller. Distortion files normally have the extension \texttt{dst}.
 * \todo{normally? Können distortion files auch andere extensions als dst haben, wenn ja, welche?}
 * \todo{filename inkl. Pfad und Extension?}
 *
 * \param HC is the handle to the controller
 * \param HH is the handle to the (scan) head
 * \param filename is the file with distortion data
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:SaveDistortion
 */
EXPORTDLL int LoadDistortion(HController HC, HHead HH, const char *filename);

/**
 * Saves the current distortion to a file. Distortion files normally have the extension \texttt{dst}.
 * \todo{normally? Können distortion files auch andere extensions als dst haben, wenn ja, welche?}
 * \todo{filename inkl. Pfad und Extension?}
 *
 * \param HC is the handle to the controller
 * \param HH is the handle to the (scan) head
 * \param filename is the file for saving the distortion data
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:LoadDistortion
 */
EXPORTDLL int SaveDistortion(HController HC, HHead HH, const char *filename);

/**
 * Gets the active scan field correction for the given head.
 * \todo{Huch! In diesem Abschnitt gibt es keine "sees".}
 *
 * \param HC is the handle to the controller
 * \param HH is the handle to the (scan) head
 *
 * \return
 *     the handle to the active scan field correction
 *     ARG_INVALID_HANDLE_VALUE on failure
 */
EXPORTDLL HScanfieldCorrection GetActiveScanfieldCorrection(HController HC, HHead HH);

/**
 * Improves a given scan field correction.
 * \todo{Aha, und wozu ist das AED-file gut?}
 * \todo{filename inkl. Pfad und Extension?}
 * \todo{Huch! In diesem Abschnitt gibt es keine "sees".}
 *
 * \param HC is the handle to the controller
 * \param HSC is the handle to the scan field correction
 * \param AEDfilename is the name of the AED-file
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 */
EXPORTDLL int ImproveScanfieldCorrection(HController HC, HScanfieldCorrection HSC, char *AEDfilename);

/**
 * Registers a callback for RequestLog-events. Only 1~callback of this type is allowed. To write the request log to a file call \nameref{fct:SetRequestLogFilename}.
 * To record all requests from the beginning of the communication between controller and library call this function before calling \nameref{fct:DetectRemoteController}.
 *
 * \param callback is the callback function
 * \param userpointer is a parameter in the callback function
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:SetRequestLogFilename
 * \see \fullref{fct:DetectRemoteController
 * \see \fullref{fct:UnregisterOnRequestLog
 * \see \fullref{fct:RequestLogCallbackFunction
 */
EXPORTDLL int RegisterOnRequestLog(RequestLogCallbackFunction callback, void *userpointer);

/**
 * Unregisters a callback for RequestLog-events.
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnRequestLog
 * \see \fullref{fct:SetRequestLogFilename
 * \see \fullref{fct:RequestLogCallbackFunction
 */
EXPORTDLL int UnregisterOnRequestLog(void);

/**
 * Sets the name of the file where the RequestLog should be written to. Please note that it is possible to have the request log written to a file and also have
 * a callback for this. 
 * \todo{Wie ist das gemeint? file und callback gleichzeitig? callback für writing to file?}
 * \todo{filname, extension and path?}
 * \todo{warum "during this call"?}
 *
 * \param filename is the complete file name for the request log,newline e.g.~texttt/tmp/request.log
 * \param deletefirst deletes the file during this call if it is set to texttt1
 * \param maxSizeInMB limits the file size. If this is set to texttt0
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:UnregisterOnRequestLog
 * \see \fullref{fct:RequestLogCallbackFunction
 */
EXPORTDLL int SetRequestLogFilename(const char *filename, int deletefirst, int maxSizeInMB);

/**
 * Sets the logging level. All logging-events which are less than the current log level are supressed. Valid log levels are: \newline
 * LOG_SUCCESS\hfill <\hfill LOG_INFO\hfill <\hfill LOG_WARNING\hfill <\hfill LOG_ERROR
 *
 * \param level is the new log level
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:GetLogLevel
 * \see \fullref{fct:SetLogFilename
 * \see \fullref{fct:RegisterOnLog
 * \see \fullref{fct:Log
 */
EXPORTDLL int SetLogLevel(ARG_LOG_LEVEL level);

/**
 * Gets the logging level. All logging-events which are less than the current log level are supressed. Valid log levels are: \newline
 * LOG_SUCCESS\hfill <\hfill LOG_INFO\hfill <\hfill LOG_WARNING\hfill <\hfill LOG_ERROR
 *
 * \return
 *     current log level
 *
 * \see \fullref{fct:GetLogLevel
 * \see \fullref{fct:SetLogFilename
 * \see \fullref{fct:RegisterOnLog
 * \see \fullref{fct:Log
 */
EXPORTDLL ARG_LOG_LEVEL GetLogLevel(void);

/**
 * Registers a callback for log-events. The client application gets internal log messages of the ControllerLib as well as messages written with \nameref{fct:Log}, if the log level is not filtered.
 * (\nameref{fct:SetRequestLogFilename}).
 * \todo{"of" oder "from" the CL?}
 * \todo{"(SetRequest\-LogFilename)." steht hier unmotiviert. Kann das gelöscht werden?}
 *
 * \param callback is the callback function
 * \param userpointer is a parameter in the callback function
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:UnregisterOnLog
 * \see \fullref{fct:SetLogFilename
 * \see \fullref{fct:Log
 * \see \fullref{fct:SetLogLevel
 * \see \fullref{fct:GetLogLevel
 * \see \fullref{fct:LogCallbackFunction
 */
EXPORTDLL int RegisterOnLog(LogCallbackFunction callback, void *userpointer);

/**
 * Unregisters a callback for log-events.
 *
 * \param callback is the callback function
 * \param userpointer is a parameter in the callback function
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:RegisterOnLog
 * \see \fullref{fct:SetLogFilename
 * \see \fullref{fct:Log
 * \see \fullref{fct:SetLogLevel
 * \see \fullref{fct:GetLogLevel
 * \see \fullref{fct:LogCallbackFunction
 */
EXPORTDLL int UnregisterOnLog(void);

/**
 * Sets the name of the file where a log file should be written to. Please note that it is possible to have a log written to a file and have
 * also a callback for it.
 * \todo{Wie ist das gemeint? file und callback gleichzeitig? callback für writing to file?}
 * \todo{Hier gibt es weder einen Parameter callback noch einen Parameter user\-poin\-ter, wohl aber einen Parameter filename.}
 * \todo{filname, extension and path?}
 *
 * \param callback is the callback function
 * \param userpointer is a parameter in the callback function
 * \param filename is the complete file name for the request log, newline e.g.~texttt/tmp/request.log
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:UnregisterOnLog
 * \see \fullref{fct:SetLogFilename
 * \see \fullref{fct:LogCallbackFunction
 * \see \fullref{fct:SetLogLevel
 * \see \fullref{fct:GetLogLevel
 * \see \fullref{fct:Log
 */
EXPORTDLL int SetLogFilename(const char *filename);

/**
 * Use this function to log the flow in the client application. The function uses the internal logging of the ControllerLib and makes use of the callback or the log file. It can be used like \texttt{printf}.
 * \todo{MR: Welche optionalen Parameter sind möglich? Wie sieht das aus?}
 *
 * \param level is the log level
 * \param fmt is the format string
 * \param ... are optional parameters
 *
 * \return
 *     E_OK on success
 *     E_FAILURE on failure
 *
 * \see \fullref{fct:UnregisterOnLog
 * \see \fullref{fct:SetLogFilename
 * \see \fullref{fct:LogCallbackFunction
 * \see \fullref{fct:SetLogLevel
 * \see \fullref{fct:GetLogLevel
 */
EXPORTDLL int Log(ARG_LOG_LEVEL level, const char *fmt, ...);


} /* extern "C" */

#endif /* __ARG_CONTROLLER_LIBC__ */

