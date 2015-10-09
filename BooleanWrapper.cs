using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OvergameBot
{
  class BooleanWrapper
  {
    bool b;

    public BooleanWrapper(bool set)
    {
      this.b = set;
    }

    public bool get()
    {
      return b;
    }

    public void set(bool set)
    {
      b = set;
    }
  }
}
