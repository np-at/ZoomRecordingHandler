# ZoomRecordingHandler

A simple webhook receiver that consumes a Zoom event.recordingcompleted and uploads it to a onedrive account



## Architecture

### Operational Sequence (currently)   (BAD):
1. Zoom Webhook with file info
2. [WebhookReceiver.cs](ZoomFileManager/Controllers/WebhookReceiver.cs) (Authentication and Validation occurs)
3. Job Data added to [processing channel](ZoomFileManager/BackgroundServices/ProcessingChannel.cs)
4. Job Data Consumed by [Zoom Event Processing Service](./ZoomFileManager/BackgroundServices/ZoomEventProcessingService.cs)
5. Scoped downloading service ([RecordingManagementService](./ZoomFileManager/Services/RecordingManagementService.cs)) created to download files defined in event data
6. Download completed, Upload performed from ([RecordingManagementService](ZoomFileManager/Services/RecordingManagementService.cs) (omg this is spaghetti code incarnate, NEEDS to be separated from download handling) Event optionally sent to Slack if defined.

---
### Operational Sequence (desired):
1. Zoom Webhook received with file info by controller [WebhookReceiver.cs](ZoomFileManager/Controllers/WebhookReceiver.cs) where authentication and validation occurs

2. If successful, event is added to download processing channel [processing channel](ZoomFileManager/BackgroundServices/ProcessingChannel.cs)

3. Event is consumed from download processing channel and an appropriate download service is found and created to act on event (handler tbd)

4. If download successful, [upload jobs](ZoomFileManager/Models/UploadJobSpec.cs) are generated according to application configuration and added to the upload queue within the [processing channel](ZoomFileManager/BackgroundServices/ProcessingChannel.cs)

5. Upload events are consumed from the upload channel by [tbd]() and an appropriate scoped service is created to handle the upload (depending on configuration defined by application config)


## MISC


### Scoping

- Each download batch should be treated an as encapsulated unit.  Naming templates for both individual files and local encapsulating folders should be applied within the scope of the download process.
  - This allows the upload batches limit concern to the specifics required for their individual targets.


## Flow2

1. AddReceiver(string path)