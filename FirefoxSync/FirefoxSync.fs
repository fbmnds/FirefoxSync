namespace FirefoxSync

// Firefox Sync Service
// (formerly Firefox Weave)

// TODO
// ----
// complete functionality
// async http
// parallel tasks
// secure password store (PasswordVault?)
// secure in memory strings (IBuffer?)
// convert script into library


open System
open System.IO
open System.Net
open System.Text
open System.Security.Cryptography

// JSON
//#r @"FSharpData\FSharp.Data.dll"
open FSharp.Data
//open FSharp.Data.Json
open FSharp.Data.JsonExtensions

// Firefox Object Formats

// http://docs.services.mozilla.com/sync/objectformats.html

type URI = URI of string
    
// Firefox docs refer to GUID, but not according to RFC4122 
// https://docs.services.mozilla.com/sync/storageformat5.html#metaglobal-record
// "The Firefox client uses 12 randomly generated base64url characters, much like for WBO IDs."
type WeaveGUID = WeaveGUID of string 

// GUID according to RFC4122
type GUID = GUID of string 

type Addons = 
    { addonID       : string
      applicationID : string
      enabled       : Boolean
      source        : string }

type Bookmarks = 
    { id            : WeaveGUID
      ``type``      : string
      title         : string
      parentName    : string
      bmkUri        : URI
      tags          : string []
      keyword       : string
      description   : string
      loadInSidebar : Boolean      
      parentid      : WeaveGUID
      children      : WeaveGUID [] }

type Microsummary = 
    { generatorUri  : string
      staticTitle   : string
      title         : string
      bmkUri        : string
      description   : string
      loadInSidebar : Boolean
      tags          : string []
      keyword       : string
      parentid      : string
      parentName    : string
      predecessorid : string
      ``type``      : string }

type Query = 
    { folderName    : string
      queryId       : string
      title         : string
      bmkUri        : string
      description   : string
      loadInSidebar : Boolean
      tags          : string []
      keyword       : string
      parentid      : string
      parentName    : string
      predecessorid : string
      ``type``      : string }

type Folder = 
    { title         : string
      parentid      : string
      parentName    : string
      predecessorid : string
      ``type``      : string }

type Livemark = 
    { siteUri       : string
      feedUri       : string
      title         : string
      parentid      : string
      parentName    : string
      predecessorid : string
      ``type``      : string }

type Separator = 
    { pos           : string
      parentid      : string
      parentName    : string
      predecessorid : string
      ``type``      : string 
      children      : string [] }

type Clients = 
    { name      : string
      ``type``  : string
      commands  : string []
      version   : string
      protocols : string [] }

type ClientsPayload = 
    { name         : string
      formfactor   : string
      application  : string
      version      : string
      capabilities : string
      mpEnabled    : Boolean }

type Commands = 
    { receiverID : string
      senderID   : string
      created    : Int64
      action     : string
      data       : string }

type Forms =  
    { name  :  string
      value : string }

type HistoryTransition = 
| TRANSITION_LINK               = 1
| TRANSITION_TYPED              = 2
| TRANSITION_BOOKMARK           = 3
| TRANSITION_EMBED              = 4
| TRANSITION_REDIRECT_PERMANENT = 5
| TRANSITION_REDIRECT_TEMPORARY = 6
| TRANSITION_DOWNLOAD           = 7
| TRANSITION_FRAMED_LINK        = 8

type HistoryPayloadVisits = 
    { uri    : string
      title  : string
      visits : string [] }

type HistoryPayload = { items : HistoryPayloadVisits [] }

type History = 
    { histUri  : string
      title    : string
      visits   : HistoryPayload
      date     : Int64 // datetime of the visit
      ``type`` : HistoryTransition }
    
type Passwords = 
    { hostname      : string
      formSubmitURL : string
      httpRealm     : string
      username      : string
      password      : string
      usernameField : string
      passwordField : string }

type Preferences = 
    { value    : string
      name     : string
      ``type`` : string }

module TabsVersions =
      
    type StringOrInteger =
    | String of string
    | Integer of int 

    type Version1 = 
        { clientName : string
          tabs       : string []
          title      : string
          urlHistory : string []
          icon       : string
          lastUsed   : StringOrInteger }

    type Version2 = 
        { clientID  : string
          title     : string
          history   : string []
          lastUsed  : Int64 // Time in seconds since Unix epoch that tab was last active.
          icon      : string
          groupName : string }

type Tabs = 
| Version1 of TabsVersions.Version1
| Version2 of TabsVersions.Version2


// Firefox Sync Secrets

type Secret = 
    { email                : string
      username             : string
      password             : string
      encryptionpassphrase : string }


// CryptoKeys

type SyncKeyBundle = 
    { encryption_key : byte[] 
      hmac_key       : byte[] }

type EncryptedCollection = 
    { iv         : string
      ciphertext : string
      hmac       : string }

type CryptoKeys = { ``default`` : byte [] [] }


// MetaGlobal
    
type MetaGlobalVersionInfo = 
    { version : int
      syncID  : WeaveGUID }

type Engine = Engine of string

type MetaGlobalPayload = 
    { syncID         : WeaveGUID
      storageVersion : int      
      engines        : Map<Engine,MetaGlobalVersionInfo>
      declined       : Engine [] }

type MetaGlobal =
    { username : string       // 8 digits, what kind of mapping?
      payload  : MetaGlobalPayload
      id       : string       // "global"
      modified : float }


// Error handling

type ErrorLabel = ErrorLabel of string
type Stacktrace = Stacktrace of string
type Error = ErrorLabel * Stacktrace

type FirefoxSyncMessage = 
    // Http error messages
    | InvalidUrl
    | SendDataError of Error
    | GetResponseError of Error
    | InvalidCredentials
    | HttpTimeOut
    // Other error messages
    | ReadSecretFileError of Error
    | ReadFileError of Error
    | WriteFileError of Error
    | ClusterUrlError of Error
    | EncryptedCollectionParseError of Error
    | FirstCryptoKeyError of Error
    | DecryptCryptoKeysError of Error
    | GetCryptoKeysFromStringError of Error
    | GetCryptoKeysError of Error
    | GetCryptoKeysFromFileError of Error
    | DecryptCollectionError of Error
    | GetBookmarksError of Error
    | ParseMetaGlobalPayloadError of Error
    | ParseMetaGlobalError of Error
    | Base32DecodeError of Error
    | CyclicBookmarkFolders of Error
    | UnescapeJsonStringError of Error


type Result<'TEntity> =
    | Success of 'TEntity
    | Failure of FirefoxSyncMessage list


// Logging Interface

type LogMessageBaseType =
    | String of string
    | Integer of int
    | Integer64 of int64
    | Float of float


type LogMessage = seq<LogMessageBaseType>

type ILogger =
    inherit System.IDisposable
    abstract member Log: string -> LogMessage -> unit
    
    