namespace BBTAG_IM.Core.CheatEngine;

public static class BridgeScript
{
    public const string EnableCommand = "enable";
    public const string DisableCommand = "disable";
    public const string StateCommand = "state";
    public const string StatusCommand = "status";
    public const string FailuresCommand = "failures";
    public const string CancelCommand = "cancel";
    public const string QuitCommand = "quit";
    public const string ShowCommand = "show";
    public const string HideCommand = "hide";
    public const string OkReply = "ok";
    public const string MissingReply = "missing";
    public const char IdSeparator = ',';
    public const char FailureSeparator = '\n';
    public const char ReasonSeparator = '\t';

    private const string PipePrefix = "BBTAG_IM_";
    private const int ReleaseIntervalMilliseconds = 15;
    private const int WaitPollMilliseconds = 50;
    private const int SettleTicks = 10;

    public static string PipeName(int processId) => PipePrefix + processId;

    public static string For(int processId) => $$"""
        if formVariables == nil then
          formVariables = setmetatable({}, { __index = function() return 0 end })
        end
        openProcess({{processId}})

        local bbtagImHeld = {}
        local bbtagImReleased = {}
        local bbtagImInFlight = {}
        local bbtagImFailures = {}
        local bbtagImOrder = {}
        local bbtagImGating = true
        local bbtagImTicking = false
        local bbtagImWaiting = 0
        local bbtagImCancelWaits = false
        local bbtagImFailureSeparator = string.char({{(int)FailureSeparator}})
        local bbtagImReasonSeparator = string.char({{(int)ReasonSeparator}})

        local function bbtagImCount(items)
          local count = 0
          for _ in pairs(items) do
            count = count + 1
          end
          return count
        end

        local function bbtagImReason(text)
          local reason = string.gsub(tostring(text or ""), "%s+", " ")
          return reason
        end

        local function bbtagImOrderOf(id)
          return bbtagImOrder[id] or math.huge
        end

        local function bbtagImFail(record, reason)
          if bbtagImFailures[record.ID] == nil then
            bbtagImFailures[record.ID] = bbtagImReason(reason)
          end
        end

        local function bbtagImGate(record, before)
          if not before or not bbtagImGating or bbtagImReleased[record.ID] then
            return true
          end
          bbtagImHeld[record.ID] = record
          return false
        end

        local function bbtagImRecordFailure(failed, reason, reasonText)
          bbtagImFailures[failed.ID] = bbtagImReason(reasonText)
          return false
        end

        local function bbtagImNextHeld()
          local nextRecord = nil
          for id, record in pairs(bbtagImHeld) do
            if nextRecord == nil or bbtagImOrderOf(id) < bbtagImOrderOf(nextRecord.ID) then
              nextRecord = record
            end
          end
          return nextRecord
        end

        local function bbtagImRelease(record)
          bbtagImHeld[record.ID] = nil
          bbtagImReleased[record.ID] = true
          bbtagImFailures[record.ID] = nil
          bbtagImInFlight[record.ID] = { record = record, settleTicks = 0 }
          local released, problem = pcall(function() record.Active = true end)
          if not released then
            bbtagImFail(record, problem)
          end
        end

        local function bbtagImSettle()
          for id, flight in pairs(bbtagImInFlight) do
            local record = flight.record
            if record.AsyncProcessing then
              flight.settleTicks = 0
            else
              flight.settleTicks = flight.settleTicks + 1
              if record.Active then
                bbtagImInFlight[id] = nil
              elseif bbtagImFailures[id] ~= nil or flight.settleTicks >= {{SettleTicks}} then
                bbtagImFail(record, "Cheat Engine did not say why.")
                bbtagImInFlight[id] = nil
              end
            end
          end
        end

        local function bbtagImTick()
          bbtagImSettle()
          if bbtagImCount(bbtagImInFlight) > bbtagImWaiting then
            return
          end
          local record = bbtagImNextHeld()
          if record ~= nil then
            bbtagImRelease(record)
          end
        end

        local function bbtagImStopGating()
          bbtagImGating = false
          for _, record in pairs(bbtagImHeld) do
            bbtagImRelease(record)
          end
        end

        local function bbtagImInstallGate()
          local list = getAddressList()
          for index = 0, list.Count - 1 do
            local record = list.getMemoryRecord(index)
            bbtagImOrder[record.ID] = index
            if record.Type == vtAutoAssembler then
              record.OnActivate = bbtagImGate
              record.OnActivationFailure = bbtagImRecordFailure
            end
          end
        end

        local function bbtagImPoll(conditionFunc, timeout)
          local deadline = getTickCount() + timeout
          while conditionFunc() == false and not GeneralTools.WaitCanceled and not bbtagImCancelWaits and getTickCount() < deadline do
            sleep({{WaitPollMilliseconds}})
          end
          return conditionFunc()
        end

        local bbtagImTableWait = GeneralTools ~= nil and GeneralTools.WaitForCondition or nil

        local function bbtagImWaitForCondition(conditionFunc, times, delay)
          if inMainThread() then
            return bbtagImTableWait(conditionFunc, times, delay)
          end
          bbtagImWaiting = bbtagImWaiting + 1
          local finished, result = pcall(bbtagImPoll, conditionFunc, times * delay)
          bbtagImWaiting = math.max(bbtagImWaiting - 1, 0)
          if bbtagImWaiting == 0 then
            bbtagImCancelWaits = false
          end
          if not finished then
            error(result, 0)
          end
          return result
        end

        if bbtagImTableWait ~= nil then
          GeneralTools.WaitForCondition = bbtagImWaitForCondition
        end

        bbtagImInstallGate()

        local bbtagImTimer = createTimer(nil, false)
        bbtagImTimer.Interval = {{ReleaseIntervalMilliseconds}}
        bbtagImTimer.OnTimer = function(timer)
          if bbtagImTicking then
            return
          end
          bbtagImTicking = true
          local ticked = pcall(bbtagImTick)
          bbtagImTicking = false
          if not ticked then
            timer.Enabled = false
            bbtagImStopGating()
          end
        end
        bbtagImTimer.Enabled = true

        local function bbtagImActiveScripts()
          local list = getAddressList()
          local active = {}
          for index = 0, list.Count - 1 do
            local record = list.getMemoryRecord(index)
            if record.Active and record.Type == vtAutoAssembler then
              active[#active + 1] = tostring(record.ID)
            end
          end
          return table.concat(active, "{{IdSeparator}}")
        end

        local function bbtagImStatus()
          local list = getAddressList()
          local running = 0
          for index = 0, list.Count - 1 do
            if list.getMemoryRecord(index).AsyncProcessing then
              running = running + 1
            end
          end
          return string.format("%d{{IdSeparator}}%d{{IdSeparator}}%d", bbtagImCount(bbtagImHeld), running, bbtagImWaiting)
        end

        local function bbtagImFailureReport()
          local list = getAddressList()
          local failed = {}
          for id in pairs(bbtagImFailures) do
            local record = list.getMemoryRecordByID(id)
            if record ~= nil and not record.Active and not record.AsyncProcessing and bbtagImHeld[id] == nil and bbtagImInFlight[id] == nil then
              failed[#failed + 1] = id
            end
          end
          table.sort(failed, function(left, right) return bbtagImOrderOf(left) < bbtagImOrderOf(right) end)
          local lines = {}
          for _, id in ipairs(failed) do
            lines[#lines + 1] = tostring(id) .. bbtagImReasonSeparator .. bbtagImFailures[id]
          end
          return table.concat(lines, bbtagImFailureSeparator)
        end

        local function bbtagImHandle(request)
          local command, argument = string.match(request, "^(%a+) ?(%d*)$")
          if command == "{{StateCommand}}" then
            return bbtagImActiveScripts()
          end
          if command == "{{StatusCommand}}" then
            return bbtagImStatus()
          end
          if command == "{{FailuresCommand}}" then
            return bbtagImFailureReport()
          end
          if command == "{{CancelCommand}}" then
            bbtagImCancelWaits = true
            return "{{OkReply}}"
          end
          if command == "{{EnableCommand}}" or command == "{{DisableCommand}}" then
            local record = getAddressList().getMemoryRecordByID(tonumber(argument))
            if record == nil then
              return "{{MissingReply}}"
            end
            if command == "{{EnableCommand}}" then
              bbtagImReleased[record.ID] = nil
              bbtagImFailures[record.ID] = nil
              bbtagImCancelWaits = false
            end
            record.Active = command == "{{EnableCommand}}"
            return "{{OkReply}}"
          end
          if command == "{{ShowCommand}}" then
            unhideMainCEwindow()
            return "{{OkReply}}"
          end
          if command == "{{HideCommand}}" then
            hideAllCEWindows()
            return "{{OkReply}}"
          end
          if command == "{{QuitCommand}}" then
            closeCE()
          end
          return "{{OkReply}}"
        end

        createThread(function(thread)
          local pipe = connectToPipe("{{PipeName(processId)}}")
          if pipe == nil then
            return
          end
          while true do
            local size = pipe.readDword()
            if size == nil then
              break
            end
            local reply = synchronize(bbtagImHandle, pipe.readString(size))
            pipe.writeDword(#reply)
            if #reply > 0 then
              pipe.writeString(reply)
            end
          end
          pipe.destroy()
        end)
        """;
}
