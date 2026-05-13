using System;
using System.Diagnostics.CodeAnalysis;
namespace xyMessageFactory.Messages;

//public interface IMessages 
//{
//}
public interface ICollectionMessages : IAbsoluteMessageProvider
{
    /// <summary>
    /// IEnumerable is empty
    /// </summary>
    /// <param name="nameOfTheIEnumerable"></param>
    /// <returns></returns>
    public string EmptyIEnumerable(string? nameOfTheIEnumerable = null);


    /// <summary>
    /// Collection is empty
    /// </summary>
    /// <param name="nameOfTheCollection"></param>
    /// <returns></returns>
    public string EmptyCollection(string? nameOfTheCollection = null);


    /// <summary>
    /// List is empty
    /// </summary>
    /// <param name="nameOfTheList"></param>
    /// <returns></returns>
    public string EmptyList(string? nameOfTheList = null);

    /// <summary>
    /// Array is empty
    /// </summary>
    /// <param name="nameOfTheArray"></param>
    /// <returns></returns>
    public string EmptyArray(string? nameOfTheArray = null);

    /// <summary>
    /// Dictionary is empty
    /// </summary>
    /// <param name="nameOfDictionary"></param>
    /// <returns></returns>
    public string EmptyDictionary(string? nameOfDictionary = null);
}

public interface IStreamMessages : IAbsoluteMessageProvider
{
    public string FileStreamError(string? file = null);
    public string FileStreamSuccess(string? file = null);
}

public interface IPathMessages : IAbsoluteMessageProvider
{
    public string PathNotFound(string? path = null);
}

public interface IFileOperationMessages : IAbsoluteMessageProvider
{
    public string FileNotFound(string? file = null);

    public string FileAccessDenied(string? file = null);

    public string FileReadError(string? file = null);

    public string FileContentError(string? file = null);

}

public interface ISerializationMessages : IAbsoluteMessageProvider
{
    /// <summary>
    /// Successfully serialized the target
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public string SerializationSuccess(object? target = null);

    /// <summary>
    /// Successfully serialized the target to file
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    public string SerializationSuccess(string fileName, object? target = null);

    /// <summary>
    /// Successfully serialized the target
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="targets"></param>
    /// <returns></returns>
    public string SerializationSuccess(string fileName, params object[] targets);


    /// <summary>
    /// Failed to serialize the target
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public string SerializationFail(string? name = null);


    /// <summary>
    /// Failed to serialize the target to file
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public string SerializationFail(string fileName, string? name = null);

    /// <summary>
    /// Successfully deserialized the target
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public string DeserializationSuccess(object? target = null);

    /// <summary>
    /// Failed to deserialize the target
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public string DeserializationFail(string? name = null);

    /// <summary>
    /// Failed to deserialize the target to file
    /// </summary>
    /// <param name="name"></param>
    /// <param name="fileName"></param>
    /// <returns></returns>
    public string DeserializationFail(string fileName, string? name = null);


}

public interface IParameterMessages : IAbsoluteMessageProvider
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="paramName"></param>
    /// <returns></returns>
    public string ParameterValid(string? paramName = null);

    /// <summary>
    /// Parameter is incorrect
    /// </summary>
    /// <param name="paramName"></param>
    /// <returns></returns>
    public string ParameterInvalid(string? paramName = null);

    /// <summary>
    /// Lists the given parameters below each other
    /// </summary>
    /// <param name="paramNames"></param>
    /// <returns></returns>
    public string ParametersInvalid(string[]? paramNames = null);


    /// <summary>
    /// Parameter is null
    /// </summary>
    /// <param name="paramName"></param>
    /// <returns></returns>
    public string ParameterNull(string? paramName = null);

    /// <summary>
    /// Lists the given parameters below each other
    /// </summary>
    /// <param name="paramNames"></param>
    /// <returns></returns>
    public string ParametersNull(string[]? paramNames = null);

    /// <summary>
    /// Parameter is null or invalid
    /// </summary>
    /// <param name="paramName"></param>
    /// <returns></returns>
    public string ParameterNullOrInvalid(string? paramName = null);


    /// <summary>
    /// Lists the given parameters below each other
    /// </summary>
    /// <param name="paramNames"></param>
    /// <returns></returns>
    public string ParametersNullOrInvalid(string[]? paramNames = null);


    /// <summary>
    /// Invalid ID
    /// </summary>
    /// <returns></returns>
    public string InvalidID(string ID);

    /// <summary>
    /// Valid ID
    /// </summary>
    /// <returns></returns>
    public string ValidID(string? ID = null);

    /// <summary>
    /// Username or password is wrong
    /// </summary>
    /// <returns></returns>
    public string WrongUserNameOrPassword(string? userName = null);
    /// <summary>
    /// Username is wrong
    /// </summary>
    /// <returns></returns>
    public string WrongUserName(string? userName = null);
    /// <summary>
    /// Password is wrong
    /// </summary>
    /// <returns></returns>
    public string WrongPassword(string? userName = null);

}

public interface IDbConnectionMessages : IAbsoluteMessageProvider
{
    public string ConnectionStringNotFound(string? name = null);

    public string DatabaseConnectionFailed(string? dbName = null);

    public string DatabaseQueryError(string? query = null);
}

public interface IModelStateMessages : IAbsoluteMessageProvider
{
    public string ModelUnvalidated(string? modelName = null);

    public string ModelValid(string? modelName = null);

    public string ModelInvalid(string? modelName = null);

    public string ModelSkipped(string? modelName = null);

}

public interface ICrudMessages : IAbsoluteMessageProvider
{
    public string Created([MaybeNull] int? ID = null);

    public string Created([MaybeNull] string? name = null);

    public string NotCreated([MaybeNull] int? ID = null);

    public string NotCreated([MaybeNull] string? name = null);

    public string Read();

    public string NotRead(int? ID = null);

    public string Updated([MaybeNull] int? ID = null);

    public string Updated([MaybeNull] string? name = null);

    public string NotUpdated([MaybeNull] int? ID = null);

    public string NotUpdated([MaybeNull] string? name = null);

    public string Deleted([MaybeNull] int? ID = null);

    public string NotDeleted([MaybeNull] int? ID = null);

}

public interface IEfCoreMessages : IAbsoluteMessageProvider
{
    public string EntryNotFound(object ID);

    public string EntryList();
    public string EntryEmptyList();

    public string EntryCreated();
    public string EntryNotCreated();

    public string EntryAdded(string? contextName = null);
    public string EntryCreatedAndAdded(string? contextName = null);

    public string EntryUpdated([MaybeNull] string? name = null);
    public string EntryNotUpdated([MaybeNull] string? name = null);

    public string EntryRemoved([MaybeNull] string? name = null);
    public string EntryNotRemoved([MaybeNull] string? name = null);

    public string ContextSaved(string name, int? count);
    public string ContextNotSaved([MaybeNull] string? name = null);

}

public interface ISecurityMessages : IAbsoluteMessageProvider
{
    public string EncryptionFailed(string? target = null);

    public string DecryptionFailed(string? target = null);

    public string InvalidCertificate(string? cert = null);
}

public interface IKeyHandlingMessages : IAbsoluteMessageProvider
{
    public string KeySet(string? key = null);
    public string KeyNotSet(string? key = null);

    public string KeyFound(string? key = null);
    public string KeyNotFound(string? key = null);

}

public interface ITokenHandlingMessages : IAbsoluteMessageProvider
{
    public string TokenGenerated(string? tokenID = null);
    public string TokenNotGenerated(string? tokenID = null);
    public string TokenExpired(string? tokenID = null);
}

public interface INetworkMessages : IAbsoluteMessageProvider
{
    public string NetworkUnavailable(string? host = null);

    public string HostUnreachable(string? host = null);

    public string TimeoutOccurred(string? operation = null);

}


public interface IUserMessages : IAbsoluteMessageProvider
{
    public string UserNotFound(string? user = null);

    public string UserAlreadyExists(string? user = null);

    public string UserLockedOut(string? user = null);

}

public interface ILoginMessages : IAbsoluteMessageProvider
{
    public string LoginStart(string url);

    public string LoginSuccess(string? username = null);
    public string LoginFail(string? username = null);
    public string LoginTimeout(int seconds);
    public string LoginUnexpected(Exception ex);
}


public interface IWindowMessages : IAbsoluteMessageProvider
{
    public string WindowSwitch(string handle);
    public string WindowSwitched(string title);

    public string WindowCloseSuccess(string title);
    public string WindowCloseFail(string title);
    public string WindowClosed(string handle);

    public string WindowOpened(string name);

    public string WindowNotFound(string title);

}

public interface IKeyPressMessages : IAbsoluteMessageProvider
{
    public string KeyPressed(string key);

    public string KeyCombinationPressed(string combo);

    public string MouseMovedTo(string description);

    public string MouseClicked(string description);

    public string MouseDoubleClicked(string description);

    public string MouseRightClicked(string description);

    public string MouseActionFailed(string action, string reason);
}

public interface ISystemMessages : IAbsoluteMessageProvider
{
    public string OperationFailed(string? operation = null);

    public string ConfigurationError(string? config = null);

    public string UnknownError(string? details = null);

}




