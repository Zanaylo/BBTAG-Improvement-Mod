namespace BBTAG_IM.Core.CheatEngine;

public static class BridgeScript
{
    public const string EnableCommand = "enable";
    public const string DisableCommand = "disable";
    public const string StateCommand = "state";
    public const string BusyCommand = "busy";
    public const string QuitCommand = "quit";
    public const string ShowCommand = "show";
    public const string HideCommand = "hide";
    public const string OkReply = "ok";
    public const string MissingReply = "missing";
    public const char IdSeparator = ',';

    private const string PipePrefix = "BBTAG_IM_";

    public static string PipeName(int processId) => PipePrefix + processId;

    public static string For(int processId) => $$"""
        if formVariables == nil then
          formVariables = setmetatable({}, { __index = function() return 0 end })
        end
        openProcess({{processId}})

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

        local function bbtagImScriptsTurningOn()
          local list = getAddressList()
          local turningOn = 0
          for index = 0, list.Count - 1 do
            if list.getMemoryRecord(index).AsyncProcessing then
              turningOn = turningOn + 1
            end
          end
          return tostring(turningOn)
        end

        local function bbtagImHandle(request)
          local command, argument = string.match(request, "^(%a+) ?(%d*)$")
          if command == "{{StateCommand}}" then
            return bbtagImActiveScripts()
          end
          if command == "{{BusyCommand}}" then
            return bbtagImScriptsTurningOn()
          end
          if command == "{{EnableCommand}}" or command == "{{DisableCommand}}" then
            local record = getAddressList().getMemoryRecordByID(tonumber(argument))
            if record == nil then
              return "{{MissingReply}}"
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
