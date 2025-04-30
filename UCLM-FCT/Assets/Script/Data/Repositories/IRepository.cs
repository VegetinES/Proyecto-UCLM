using System;
using System.Collections.Generic;
using Realms;

public interface IRepository<T> where T : RealmObject
{
    T GetById(string id);
    List<T> GetAll();
    void Add(T entity);
    void Update(T entity, Action<T> updateAction);
    void Delete(T entity);
    void DeleteById(string id);
}